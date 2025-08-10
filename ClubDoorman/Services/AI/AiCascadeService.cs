using Telegram.Bot.Types;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.AI;

/// <summary>
/// Сервис для выполнения каскадного AI анализа сообщений
/// </summary>
public class AiCascadeService : IAiCascadeService
{
    private readonly IAiChecks _aiChecks;
    private readonly IMessageService _messageService;
    private readonly IModerationService _moderationService;
    private readonly IUserBanService _userBanService;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IAdminNotificationService _adminNotificationService;
    private readonly ILogger<AiCascadeService> _logger;

    public AiCascadeService(
        IAiChecks aiChecks,
        IMessageService messageService,
        IModerationService moderationService,
        IUserBanService userBanService,
        ITelegramBotClientWrapper bot,
        IAdminNotificationService adminNotificationService,
        ILogger<AiCascadeService> logger)
    {
        _aiChecks = aiChecks ?? throw new ArgumentNullException(nameof(aiChecks));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
        _userBanService = userBanService ?? throw new ArgumentNullException(nameof(userBanService));
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _adminNotificationService = adminNotificationService ?? throw new ArgumentNullException(nameof(adminNotificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Выполняет AI анализ профиля пользователя при первом сообщении
    /// </summary>
    public async Task<bool> PerformAiProfileAnalysisAsync(Message message, User user, Chat chat, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("🤖 Запускаем AI анализ профиля пользователя {UserId} ({UserName})", 
            user.Id, Utils.FullName(user.FirstName, user.LastName));
        _logger.LogDebug("🔍 TRACE: PerformAiProfileAnalysis начат для пользователя {UserId}", user.Id);
        
        try
        {
            // Передаем первое сообщение в AI анализ
            var messageText = message.Text ?? message.Caption ?? "";
            var result = await _aiChecks.GetAttentionBaitProbability(user, messageText);
            _logger.LogDebug("🔍 TRACE: AiChecks.GetAttentionBaitProbability завершен для пользователя {UserId}", user.Id);
            _logger.LogInformation("🤖 AI анализ профиля: пользователь {UserId}, вероятность={Probability}, причина={Reason}", 
                user.Id, result.SpamProbability.Probability, result.SpamProbability.Reason);

            // Проверяем на банальность приветствия
            var isBoringGreeting = AiChecks.IsBoringGreeting(messageText);
            
            // Логика: высокий спам (>=0.9) действует всегда, средний (>=0.75) только с банальным приветствием
            var isHighSpam = result.SpamProbability.Probability >= Consts.LlmHighProbability; // >= 0.9
            var isMediumSpamWithBoringGreeting = result.SpamProbability.Probability >= Consts.LlmLowProbability && isBoringGreeting; // >= 0.75 + банальное
            var shouldTriggerAction = isHighSpam || isMediumSpamWithBoringGreeting;
            
            _logger.LogDebug("🤖 AI анализ: вероятность={Probability}, банальное приветствие={IsBoringGreeting}, высокий спам={IsHighSpam}, действие={ShouldTrigger}", 
                result.SpamProbability.Probability, isBoringGreeting, isHighSpam, shouldTriggerAction);

            // Проверяем пороги вероятности спама + банальность приветствия
            if (shouldTriggerAction) // >= 0.75 + банальное приветствие
            {
                _logger.LogWarning("🚫 AI определил подозрительный профиль: пользователь {UserId}, вероятность={Probability}, банальное приветствие={IsBoringGreeting}", 
                    user.Id, result.SpamProbability.Probability, isBoringGreeting);

                // Сначала отправляем уведомление в админ-чат, потом удаляем сообщение
                var shouldDeleteMessage = result.SpamProbability.Probability >= Consts.LlmHighProbability; // >= 0.9
                var automaticAction = shouldDeleteMessage 
                    ? "🗑️ Сообщение удалено + 🔇 Read-Only на 10 минут" 
                    : "🔇 Read-Only на 10 минут (сообщение оставлено)";
                    
                var aiProfileData = new AiProfileAnalysisData(
                    user, 
                    chat, 
                    result.SpamProbability.Probability, 
                    result.SpamProbability.Reason, 
                    result.NameBio, 
                    messageText, 
                    result.Photo, 
                    message.MessageId,
                    automaticAction
                );
                
                // Отправляем уведомление ПЕРЕД удалением сообщения (включая пересылку)
                await _messageService.SendAiProfileAnalysisAsync(aiProfileData, cancellationToken);

                // Теперь удаляем сообщение (если нужно)
                if (shouldDeleteMessage)
                {
                    try
                    {
                        await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                        _logger.LogInformation("🗑️ Сообщение удалено из-за высокой вероятности спама: {Probability:F2}", result.SpamProbability.Probability);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось удалить сообщение при AI анализе");
                    }
                }

                return true; // Пользователь получил ограничения
            }
            else
            {
                _logger.LogDebug("🤖✅ AI анализ профиля: пользователь безопасен, вероятность={Probability}", result.SpamProbability.Probability);
                return false; // Всё хорошо
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при AI анализе профиля пользователя {UserId}", user.Id);
            return false; // При ошибке считаем, что всё хорошо
        }
    }

    /// <summary>
    /// Выполняет каскадный AI анализ на основе ML оценки
    /// </summary>
    public async Task HandleAiCascadeAnalysisAsync(Message message, User user, double mlScore, bool isSilentMode, CancellationToken cancellationToken = default)
    {
        var messageText = message.Text ?? message.Caption ?? "";
        var chat = message.Chat;
        
        if (string.IsNullOrWhiteSpace(messageText))
        {
            _logger.LogWarning("🤖 AI каскадный анализ: пропускаем медиа без текста от {User}", Utils.FullName(user));
            // Для медиа без текста отправляем в ручную проверку
            await _adminNotificationService.DontDeleteButReportMessageAsync(message, user, isSilentMode, cancellationToken);
            return;
        }

        try
        {
            _logger.LogInformation("🤖🔗 КАСКАДНЫЙ АНАЛИЗ: ML дал скор {MlScore}, запускаем AI для пользователя {User}: '{Text}'", 
                mlScore, Utils.FullName(user), messageText.Substring(0, Math.Min(messageText.Length, 100)));

            // Запускаем комплексный AI анализ (профиль + сообщение + ML данные)
            var aiResult = await _aiChecks.GetCascadeAnalysisProbability(message, user, mlScore, false).AsTask().WaitAsync(TimeSpan.FromSeconds(30));
            var aiProbability = aiResult.Probability;
            var aiReason = aiResult.Reason ?? "Нет объяснения";

            _logger.LogInformation("🤖✅ AI каскадный анализ завершен: пользователь {User}, ML={MlScore}, AI={AiScore}, причина: {AiReason}", 
                Utils.FullName(user), mlScore, aiProbability, aiReason);

            // Принимаем решение на основе AI анализа
            if (aiProbability >= 0.8) // Высокая вероятность спама по AI
            {
                _logger.LogWarning("🤖🚫 AI каскадный анализ: определен спам (AI={AiScore}), удаляем сообщение", aiProbability);
                await _adminNotificationService.DeleteAndReportMessageAsync(message, $"AI каскадный анализ: спам (ML={mlScore:F2}, AI={aiProbability:F2})", isSilentMode, cancellationToken);
                
                // Отслеживаем нарушения для повторных банов
                await _userBanService.TrackViolationAndBanIfNeededAsync(message, user, $"AI каскадный анализ: спам (ML={mlScore:F2}, AI={aiProbability:F2})", cancellationToken);
            }
            else if (aiProbability >= 0.4) // Подозрительно - требует внимания админов
            {
                _logger.LogInformation("🤖❓ AI каскадный анализ: подозрительное сообщение (AI={AiScore}), отправляем админам", aiProbability);
                await _adminNotificationService.DontDeleteButReportMessageAsync(message, user, isSilentMode, cancellationToken);
            }
            else // AI считает сообщение безопасным
            {
                _logger.LogInformation("🤖✅ AI каскадный анализ: сообщение безопасно (AI={AiScore}), разрешаем", aiProbability);
                
                // Засчитываем как хорошее сообщение
                await _moderationService.IncrementGoodMessageCountAsync(user, chat, messageText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при AI каскадном анализе для пользователя {UserId}", user.Id);
            
            // При ошибке AI отправляем в ручную проверку
            await _adminNotificationService.DontDeleteButReportMessageAsync(message, user, isSilentMode, cancellationToken);
        }
    }
}