using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Logging;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Services.AI;

public class AiCascadeService : IAiCascadeService
{
    private readonly ILogger<AiCascadeService> _logger;
    private readonly IMessageService _messageService;
    private readonly IAppConfig _appConfig;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IAiChecks _aiChecks;
    private readonly GlobalStatsManager _globalStatsManager;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IUserBanService _userBanService;
    private readonly IModerationService _moderationService;

    public AiCascadeService(
        ILogger<AiCascadeService> logger,
        IMessageService messageService,
        IAppConfig appConfig,
        ITelegramBotClientWrapper bot,
        IAiChecks aiChecks,
        GlobalStatsManager globalStatsManager,
        IUserFlowLogger userFlowLogger,
        IUserBanService userBanService,
        IModerationService moderationService)
    {
        _logger = logger;
        _messageService = messageService;
        _appConfig = appConfig;
        _bot = bot;
        _aiChecks = aiChecks;
        _globalStatsManager = globalStatsManager;
        _userFlowLogger = userFlowLogger;
        _userBanService = userBanService;
        _moderationService = moderationService;
    }

    public async Task<bool> PerformAiProfileAnalysisAsync(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🤖 Запускаем AI анализ профиля пользователя {UserId} ({UserName})", 
            user.Id, FullName(user.FirstName, user.LastName));
        _logger.LogDebug("🔍 TRACE: PerformAiProfileAnalysis начат для пользователя {UserId}", user.Id);
        
        try
        {
            // ФИКС: Передаем первое сообщение в AI анализ
            var messageText = message.Text ?? message.Caption ?? "";
            var result = await _aiChecks.GetAttentionBaitProbability(user, messageText);
            _logger.LogDebug("🔍 TRACE: AiChecks.GetAttentionBaitProbability завершен для пользователя {UserId}", user.Id);
            _logger.LogInformation("🤖 AI анализ профиля: пользователь {UserId}, вероятность={Probability}, причина={Reason}", 
                user.Id, result.SpamProbability.Probability, result.SpamProbability.Reason);

            // ФИКС: Восстанавливаем проверку на банальность приветствия
            var isBoringGreeting = AiChecks.IsBoringGreeting(messageText);
            
            // ИСПРАВЛЕННАЯ ЛОГИКА: высокий спам (>=0.9) действует всегда, средний (>=0.75) только с банальным приветствием
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

                // ФИКС: Сначала отправляем уведомление в админ-чат, потом удаляем сообщение
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
                else
                {
                    _logger.LogInformation("💬 Сообщение НЕ удалено (средняя вероятность): {Probability:F2}", result.SpamProbability.Probability);
                }

                // Даем ридонли на 10 минут в любом случае
                try
                {
                    var untilDate = DateTime.UtcNow.AddMinutes(10);
                    await _bot.RestrictChatMember(
                        chat.Id, 
                        user.Id, 
                        new ChatPermissions
                        {
                            CanSendMessages = false,
                            CanSendAudios = false,
                            CanSendDocuments = false,
                            CanSendPhotos = false,
                            CanSendVideos = false,
                            CanSendVideoNotes = false,
                            CanSendVoiceNotes = false,
                            CanSendPolls = false,
                            CanSendOtherMessages = false,
                            CanAddWebPagePreviews = false,
                            CanChangeInfo = false,
                            CanInviteUsers = false,
                            CanPinMessages = false,
                            CanManageTopics = false
                        },
                        untilDate: (DateTime?)untilDate,
                        cancellationToken: cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось дать ридонли пользователю");
                }



                _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");
                _userFlowLogger.LogUserRestricted(user, chat, $"AI анализ профиля: {result.SpamProbability.Reason}", TimeSpan.FromMinutes(10));
                return true; // Возвращаем true - пользователь получил ограничения
            }
            else
            {
                _logger.LogDebug("✅ AI анализ: профиль пользователя {UserId} выглядит безопасно (вероятность={Probability}, банальное приветствие={IsBoringGreeting})", 
                    user.Id, result.SpamProbability.Probability, isBoringGreeting);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Ошибка при AI анализе профиля пользователя {UserId}", user.Id);
            // Продолжаем выполнение даже при ошибке AI анализа
        }

        return false; // Возвращаем false - профиль безопасен, продолжаем модерацию
    }
    public async Task HandleAiCascadeAnalysisAsync(Message message, User user, double mlScore, bool isSilentMode, CancellationToken cancellationToken)
    {
        var messageText = message.Text ?? message.Caption ?? "";
        var chat = message.Chat;
        
        if (string.IsNullOrWhiteSpace(messageText))
        {
            _logger.LogWarning("🤖 AI каскадный анализ: пропускаем медиа без текста от {User}", Utils.FullName(user));
            // Для медиа без текста отправляем в ручную проверку
            await DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
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
                await DeleteAndReportMessage(message, $"AI каскадный анализ: спам (ML={mlScore:F2}, AI={aiProbability:F2})", isSilentMode, cancellationToken);
                
                // Отслеживаем нарушения для повторных банов
                await _userBanService.TrackViolationAndBanIfNeededAsync(message, user, $"AI каскадный анализ: спам (ML={mlScore:F2}, AI={aiProbability:F2})", cancellationToken);
            }
            else if (aiProbability >= 0.4) // Подозрительно - требует внимания админов
            {
                _logger.LogInformation("🤖❓ AI каскадный анализ: подозрительное сообщение (AI={AiScore}), отправляем админам", aiProbability);
                await DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
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
            await DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
        }
    }
}
