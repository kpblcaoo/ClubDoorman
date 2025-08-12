using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Models;

namespace ClubDoorman.Features.Moderation;

/// <summary>
/// Фасад для функциональности модерации
/// <tags>moderation, facade, coordination, thin-layer</tags>
/// </summary>
public class ModerationFacade : IModerationFacade
{
    private readonly IModerationPolicy _moderationPolicy;
    private readonly INotificationService _notificationService;
    private readonly IAiCascadeService _aiCascadeService;
    private readonly IUserBanService _userBanService;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly ILogger<ModerationFacade> _logger;
    private readonly IMessageService _messageService;

    public ModerationFacade(
        IModerationPolicy moderationPolicy,
        IUserBanService userBanService,
        IUserFlowLogger userFlowLogger,
        ILogger<ModerationFacade> logger,
        IMessageService messageService,
        INotificationService notificationService,
        IAiCascadeService aiCascadeService)
    {
        _moderationPolicy = moderationPolicy;
        _userBanService = userBanService;
        _userFlowLogger = userFlowLogger;
        _logger = logger;
        _messageService = messageService;
        _notificationService = notificationService;
        _aiCascadeService = aiCascadeService;
    }

    public Task<ModerationResult> CheckMessageAsync(Message message)
    {
        return _moderationPolicy.CheckMessageAsync(message);
    }

    public Task<ModerationResult> CheckUserNameAsync(User user)
    {
        return _moderationPolicy.CheckUserNameAsync(user);
    }

    public Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText)
    {
        return _moderationPolicy.IncrementGoodMessageCountAsync(user, chat, messageText);
    }

    public bool IsUserApproved(long userId, long? chatId = null)
    {
        return _moderationPolicy.IsUserApproved(userId, chatId);
    }

    public bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled)
    {
        return _moderationPolicy.SetAiDetectForSuspiciousUser(userId, chatId, enabled);
    }

    public (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats()
    {
        return _moderationPolicy.GetSuspiciousUsersStats();
    }

    public List<(long UserId, long ChatId)> GetAiDetectUsers()
    {
        return _moderationPolicy.GetAiDetectUsers();
    }

    public Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message)
    {
        return _moderationPolicy.CheckAiDetectAndNotifyAdminsAsync(user, chat, message);
    }

    public Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId)
    {
        return _moderationPolicy.UnrestrictAndApproveUserAsync(userId, chatId);
    }

    public void CleanupUserFromAllLists(long userId, long chatId)
    {
        _moderationPolicy.CleanupUserFromAllLists(userId, chatId);
    }

    public Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null)
    {
        return _moderationPolicy.BanAndCleanupUserAsync(userId, chatId, messageIdToDelete);
    }

    public Task ExecuteModerationActionAsync(Message message, ModerationResult result)
    {
        return _moderationPolicy.ExecuteModerationActionAsync(message, result);
    }

    /// <summary>
    /// Обрабатывает сообщение пользователя на основе результата модерации
    /// <tags>moderation, message-handling, action-execution</tags>
    /// </summary>
    /// <param name="message">Сообщение для обработки</param>
    /// <param name="user">Пользователь</param>
    /// <param name="chat">Чат</param>
    /// <param name="moderationResult">Результат модерации</param>
    /// <param name="isSilentMode">Тихий режим</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task HandleUserMessageAsync(
        Message message, 
        User user, 
        Chat chat, 
        ModerationResult moderationResult, 
        bool isSilentMode, 
        CancellationToken cancellationToken)
    {
        // PRE-CHECK: пересланные сообщения от новичков (точная копия из MessageHandler)
        if (Config.DeleteForwardedMessages && message.ForwardOrigin != null)
        {
            _logger.LogInformation("🔄 Удаление пересланного сообщения от новичка {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})",
                Utils.FullName(user), user.Id, chat.Title ?? "-", chat.Id);
            Message? notificationMessage = null;
            try
            {
                notificationMessage = await _messageService.SendUserNotificationWithReplyAsync(user, chat, UserNotificationType.MessageDeleted,
                    new SimpleNotificationData(user, chat, "пересланные сообщения от новичков не разрешены"),
                    new ReplyParameters { MessageId = message.MessageId },
                    cancellationToken);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                        await _messageService.DeleteMessage(chat.Id, notificationMessage.MessageId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось удалить уведомление пользователю");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить предупреждение пользователю");
            }
            try
            {
                await _messageService.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить пересланное сообщение от новичка");
            }
            return;
        }

        switch (moderationResult.Action)
        {
            case ModerationAction.Allow:
                _logger.LogDebug("Сообщение разрешено: {Reason}", moderationResult.Reason);
                var allowedMessageText = message.Text ?? message.Caption ?? "";
                // AI-профайл-анализ (точная копия из MessageHandler)
                var profileAnalysisResult = await _aiCascadeService.PerformAiProfileAnalysisAsync(message, user, chat, cancellationToken);
                if (profileAnalysisResult)
                {
                    // Пользователь получил ограничения за подозрительный профиль, возвращаемся
                    return;
                }
                // Проверяем AI детект для подозрительных пользователей
                var aiDetectBlocked = await _moderationPolicy.CheckAiDetectAndNotifyAdminsAsync(user, chat, message);
                if (!aiDetectBlocked)
                {
                    await _moderationPolicy.IncrementGoodMessageCountAsync(user, chat, allowedMessageText);
                }
                break;

            case ModerationAction.Ban:
                _userFlowLogger.LogUserBanned(user, chat, moderationResult.Reason);
                await _userBanService.AutoBanAsync(message, moderationResult.Reason, cancellationToken);
                break;

            case ModerationAction.Delete:
                _logger.LogInformation("Удаление сообщения: {Reason}", moderationResult.Reason);
                try
                {
                    // Специальная обработка для ссылок и банальных приветствий - отправляем в лог-чат без предупреждения пользователю
                    if (moderationResult.Reason.Contains("Ссылки запрещены") || moderationResult.Reason.Contains("Банальное приветствие"))
                    {
                        await DeleteAndReportToLogChat(message, moderationResult.Reason, cancellationToken);
                    }
                    else
                    {
                        await DeleteAndReportMessage(message, moderationResult.Reason, isSilentMode, cancellationToken);
                    }
                    _logger.LogInformation("Сообщение успешно обработано для удаления");

                    // Отслеживаем нарушения для повторных банов
                    await _userBanService.TrackViolationAndBanIfNeededAsync(message, user, moderationResult.Reason, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при удалении сообщения: {Reason}", moderationResult.Reason);
                }
                break;

            case ModerationAction.Report:
                _logger.LogInformation("Отправка в админ-чат: {Reason}", moderationResult.Reason);
                await DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
                break;

            case ModerationAction.RequireManualReview:
                _logger.LogInformation("Требует ручной проверки: {Reason}", moderationResult.Reason);
                await DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
                break;

            case ModerationAction.RequireAiAnalysis:
                _logger.LogInformation("ML не уверен, запускаем AI анализ: {Reason}", moderationResult.Reason);
                await HandleAiCascadeAnalysis(message, user, moderationResult.Confidence ?? 0, isSilentMode, cancellationToken);
                break;
        }
    }

    // Вспомогательные методы для обработки сообщений
    public async Task DeleteAndReportToLogChat(Message message, string reason, CancellationToken cancellationToken)
    {
        // WRAP: delegated to NotificationService
        await _notificationService.DeleteAndReportToLogChat(message, reason, cancellationToken);
    }


    public async Task DeleteAndReportMessage(Message message, string reason, bool isSilentMode, CancellationToken cancellationToken)
    {
        // WRAP: delegated to NotificationService
        await _notificationService.DeleteAndReportMessage(message, reason, isSilentMode, cancellationToken);
    }

    public async Task DontDeleteButReportMessage(Message message, User user, bool isSilentMode, CancellationToken cancellationToken)
    {
        // WRAP: delegated to NotificationService
        await _notificationService.DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
    }

    internal async Task HandleAiCascadeAnalysis(Message message, User user, double mlScore, bool isSilentMode, CancellationToken cancellationToken)
    {
        // WRAP: delegated to AiCascadeService
        await _aiCascadeService.HandleAiCascadeAnalysisAsync(message, user, mlScore, isSilentMode, cancellationToken);
    }

    // Публичный метод для кнопок (если требуется)
    public async Task SendSuspiciousMessageWithButtons(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken)
    {
        await _messageService.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, cancellationToken);
    }
}