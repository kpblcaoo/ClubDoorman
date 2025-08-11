using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Text;
using Microsoft.Extensions.Logging;
using ClubDoorman.Services.Commands;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Models.Requests;
using ClubDoorman.Services;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.UserBan;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Extensions;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Handlers;
using ClubDoorman.Services.TextProcessing; // restored

namespace ClubDoorman.Services.Handlers;

/// <summary>
/// Обработчик сообщений
/// </summary>
public class MessageHandler : IUpdateHandler, IMessageHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IModerationService _moderationService;
    private readonly ICaptchaService _captchaService;
    private readonly IUserManager _userManager;
    private readonly ISpamHamClassifier _classifier;
    private readonly IBadMessageManager _badMessageManager;
    private readonly IAiChecks _aiChecks;
    private readonly GlobalStatsManager _globalStatsManager;
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<MessageHandler> _logger;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IMessageService _messageService;
    private readonly IChatLinkFormatter _chatLinkFormatter;
    private readonly IBotPermissionsService _botPermissionsService;
    private readonly IAppConfig _appConfig;
    private readonly IViolationTracker _violationTracker;
    private readonly IUserBanService _userBanService;
    private readonly IChannelModerationService _channelModerationService;
    private readonly IStartCommandHandler _startCommandHandler;
    private readonly ISuspiciousCommandHandler _suspiciousCommandHandler;
    private readonly ICommandRouter _commandRouter;
    private readonly ILogChatService _logChatService;
    private readonly IJoinedUserFlags _joinedUserFlags;
    private readonly IUserIndex _userIndex;

    private readonly IAiCascadeService _aiCascadeService; // injected service
    private readonly ClubDoorman.Services.Messaging.INotificationService _notificationService; // injected service (fully-qualified)
    private readonly ClubDoorman.Services.Notifications.IForwardingService _forwardingService; // injected service
    private readonly ClubDoorman.Services.Notifications.IButtonsService _buttonsService; // injected service

    /// <summary>
    /// Создает экземпляр обработчика сообщений.
    /// </summary>
    /// <param name="bot">Клиент Telegram бота</param>
    /// <param name="moderationService">Сервис модерации</param>
    /// <param name="captchaService">Сервис капчи</param>
    /// <param name="userManager">Менеджер пользователей</param>
    /// <param name="classifier">Классификатор спама</param>
    /// <param name="badMessageManager">Менеджер плохих сообщений</param>
    /// <param name="aiChecks">AI проверки</param>
    /// <param name="globalStatsManager">Менеджер глобальной статистики</param>
    /// <param name="statisticsService">Сервис статистики</param>
    /// <param name="userFlowLogger">Логгер пользовательского флоу</param>
    /// <param name="messageService">Сервис уведомлений</param>
    /// <param name="chatLinkFormatter">Форматтер ссылок на чаты</param>
    /// <param name="botPermissionsService">Сервис проверки прав бота</param>
    /// <param name="appConfig">Конфигурация приложения</param>
    /// <param name="violationTracker">Трекер нарушений</param>
    /// <param name="logger">Логгер</param>
    /// <param name="userBanService">Сервис управления банами пользователей</param>
    /// <param name="channelModerationService">Сервис модерации каналов</param>
    /// <param name="startCommandHandler">Обработчик команды /start</param>
    /// <param name="commandRouter">Маршрутизатор команд</param>
    /// <param name="logChatService">Сервис лог-чата</param>
    /// <param name="joinedUserFlags">Сервис управления флагами присоединившихся пользователей</param>
    /// <param name="userIndex">Сервис индексации пользователей</param>
    /// <exception cref="ArgumentNullException">Если любой из параметров равен null</exception>
    public MessageHandler(
        ITelegramBotClientWrapper bot,
        IModerationService moderationService,
        ICaptchaService captchaService,
        IUserManager userManager,
        ISpamHamClassifier classifier,
        IBadMessageManager badMessageManager,
        IAiChecks aiChecks,
        GlobalStatsManager globalStatsManager,
        IStatisticsService statisticsService,
        IUserFlowLogger userFlowLogger,
        IMessageService messageService,
        IChatLinkFormatter chatLinkFormatter,
        IBotPermissionsService botPermissionsService,
        IAppConfig appConfig,
        IViolationTracker violationTracker,
        ILogger<MessageHandler> logger,
        IUserBanService userBanService,
        IChannelModerationService channelModerationService,
        IStartCommandHandler startCommandHandler,
        ISuspiciousCommandHandler suspiciousCommandHandler,
        ICommandRouter commandRouter,
        ILogChatService logChatService,
        IJoinedUserFlags joinedUserFlags,
        IUserIndex userIndex,
        IAiCascadeService aiCascadeService,
        ClubDoorman.Services.Messaging.INotificationService notificationService,
        ClubDoorman.Services.Notifications.IForwardingService forwardingService,
        ClubDoorman.Services.Notifications.IButtonsService buttonsService)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
        _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _badMessageManager = badMessageManager ?? throw new ArgumentNullException(nameof(badMessageManager));
        _aiChecks = aiChecks ?? throw new ArgumentNullException(nameof(aiChecks));
        _globalStatsManager = globalStatsManager ?? throw new ArgumentNullException(nameof(globalStatsManager));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _userFlowLogger = userFlowLogger ?? throw new ArgumentNullException(nameof(userFlowLogger));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _chatLinkFormatter = chatLinkFormatter ?? throw new ArgumentNullException(nameof(chatLinkFormatter));
        _botPermissionsService = botPermissionsService ?? throw new ArgumentNullException(nameof(botPermissionsService));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _violationTracker = violationTracker ?? throw new ArgumentNullException(nameof(violationTracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userBanService = userBanService ?? throw new ArgumentNullException(nameof(userBanService));
        _channelModerationService = channelModerationService ?? throw new ArgumentNullException(nameof(channelModerationService));
        _startCommandHandler = startCommandHandler ?? throw new ArgumentNullException(nameof(startCommandHandler));
        _suspiciousCommandHandler = suspiciousCommandHandler ?? throw new ArgumentNullException(nameof(suspiciousCommandHandler));
        _commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
        _logChatService = logChatService ?? throw new ArgumentNullException(nameof(logChatService));
        _joinedUserFlags = joinedUserFlags ?? throw new ArgumentNullException(nameof(joinedUserFlags));
        _userIndex = userIndex ?? throw new ArgumentNullException(nameof(userIndex));
        _aiCascadeService = aiCascadeService ?? throw new ArgumentNullException(nameof(aiCascadeService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _forwardingService = forwardingService ?? throw new ArgumentNullException(nameof(forwardingService));
        _buttonsService = buttonsService ?? throw new ArgumentNullException(nameof(buttonsService));
    }

    /// <summary>
    /// Проверяет, может ли обработчик обработать данное обновление.
    /// </summary>
    /// <param name="update">Обновление для проверки</param>
    /// <returns>true, если обновление содержит сообщение</returns>
    public bool CanHandle(Update update)
    {
        return update?.Message != null || update?.EditedMessage != null;
    }

    /// <summary>
    /// Обрабатывает обновление, содержащее сообщение.
    /// </summary>
    /// <param name="update">Обновление для обработки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <exception cref="ArgumentNullException">Если update равен null</exception>
    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update == null) throw new ArgumentNullException(nameof(update));
        if (update.Message == null && update.EditedMessage == null) 
            throw new ArgumentNullException(nameof(update.Message));

        var message = update.EditedMessage ?? update.Message!;
        var chat = message.Chat;

        Console.WriteLine($"[DEBUG] MessageHandler.HandleAsync: получено сообщение '{message.Text}' в чате {chat.Id}");

        _logger.LogDebug("MessageHandler получил сообщение {MessageId} в чате {ChatId} от пользователя {UserId}", 
            message.MessageId, chat.Id, message.From?.Id);

        // Проверка whitelist - если активен, работаем только в разрешённых чатах
        // ИСКЛЮЧЕНИЕ: админ-чаты всегда обрабатываются (для команд /spam, /ham и т.д.)
        var isAdminChat = chat.Id == _appConfig.AdminChatId || chat.Id == _appConfig.LogAdminChatId;
        
        if (!_appConfig.IsChatAllowed(chat.Id) && !isAdminChat)
        {
            _logger.LogDebug("Чат {ChatId} ({ChatTitle}) не в whitelist - игнорируем", chat.Id, chat.Title);
            return;
        }

        // Игнорировать полностью отключённые чаты
        if (_appConfig.DisabledChats.Contains(chat.Id))
            return;

        // Проверяем тихий режим (бот без прав администратора)
        var isSilentMode = await _botPermissionsService.IsSilentModeAsync(chat.Id, cancellationToken);
        if (isSilentMode)
        {
            _logger.LogInformation("🔇 Тихий режим в чате {ChatId} ({ChatTitle}) - бот без прав администратора", chat.Id, chat.Title);
        }

        // Автоматически добавляем чат в конфиг
        ChatSettingsManager.EnsureChatInConfig(chat.Id, chat.Title);

        // Обработка команд
        if (message.Text?.StartsWith("/") == true)
        {
            Console.WriteLine($"[DEBUG] MessageHandler: обрабатываем команду '{message.Text}'");
            await HandleCommandAsync(message, cancellationToken);
            return;
        }

        Console.WriteLine($"[DEBUG] MessageHandler: не команда, продолжаем обработку");

        // Для приватных чатов обрабатываем только команды, остальное игнорируем
        if (chat.Type == ChatType.Private)
        {
            _logger.LogDebug("Приватный чат {ChatId} - обрабатываем только команды", chat.Id);
            return;
        }

        // Обработка новых участников
        if (message.NewChatMembers != null && chat.Id != _appConfig.AdminChatId)
        {
            await HandleNewMembersAsync(message, cancellationToken);
            return;
        }

        // Удаление сообщений о бане ботом
        if (message.LeftChatMember != null && message.From?.Id == _bot.BotId)
        {
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                _logger.LogDebug("Удалено сообщение о бане/исключении пользователя");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось удалить сообщение о бане/исключении");
            }
            return;
        }

        // Сообщения от каналов
        if (message.SenderChat != null)
        {
            await HandleChannelMessageAsync(message, cancellationToken);
            return;
        }

        // Обычные сообщения пользователей
        await HandleUserMessageAsync(message, isSilentMode, cancellationToken);
    }

    public async Task HandleCommandAsync(Message message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DEBUG] MessageHandler.HandleCommandAsync: команда '{message.Text}'");
        
        // Обрабатываем команду через CommandRouter
        var handled = await _commandRouter.HandleCommandAsync(message, cancellationToken);

        Console.WriteLine($"[DEBUG] MessageHandler.HandleCommandAsync: CommandRouter returned {handled}");
        
        if (handled)
        {
            _logger.LogDebug("Команда обработана через CommandRouter: {Command}", 
                message.Text?.Split(' ')[0]);
        }
        else
        {
            _logger.LogDebug("CommandRouter не смог обработать команду: {Command}", 
                message.Text?.Split(' ')[0]);
        }
    }

    // Вспомогательная функция для поиска userId по username среди недавних пользователей (по кэшу)
    internal long? TryFindUserIdByUsername(string username)
    {
        return _userIndex.TryFindUserIdByUsername(username);
    }

    public async Task HandleNewMembersAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.NewChatMembers == null)
        {
            _logger.LogDebug("Сообщение о новых участниках не содержит данных о пользователях");
            return;
        }

        foreach (var newUser in message.NewChatMembers.Where(x => x != null && !x.IsBot))
        {
            if (!_joinedUserFlags.IsUserRecentlyJoined(message.Chat.Id, newUser.Id))
            {
                _logger.LogInformation("==================== НОВЫЙ УЧАСТНИК ====================\n" +
                    "Пользователь {User} (id={UserId}, username={Username}) зашел в группу '{ChatTitle}' (id={ChatId})\n" +
                    "========================================================", 
                    Utils.FullName(newUser), newUser.Id, newUser.Username ?? "-", message.Chat.Title ?? "-", message.Chat.Id);

                _joinedUserFlags.MarkUserAsJoined(message.Chat.Id, newUser.Id);
            }

            await ProcessNewUserAsync(message, newUser, cancellationToken);
        }
    }

    public async Task ProcessNewUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            _logger.LogWarning("ProcessNewUserAsync вызван с null пользователем");
            return;
        }

        if (userJoinMessage?.Chat == null)
        {
            _logger.LogWarning("ProcessNewUserAsync вызван с null сообщением или чатом");
            return;
        }

        var chat = userJoinMessage.Chat;

        // Проверка имени пользователя
        if (_moderationService == null)
        {
            _logger.LogError("_moderationService равен null в ProcessNewUserAsync");
            return;
        }

        var nameResult = await _moderationService.CheckUserNameAsync(user);
        if (nameResult.Action == ModerationAction.Ban)
        {
            await _userBanService.BanUserForLongNameAsync(userJoinMessage, user, nameResult.Reason, null, cancellationToken);
            return;
        }
        if (nameResult.Action == ModerationAction.Report)
        {
            await _userBanService.BanUserForLongNameAsync(userJoinMessage, user, nameResult.Reason, TimeSpan.FromMinutes(10), cancellationToken);
            return;
        }

        // Проверка клубного пользователя
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (clubUser != null)
        {
            _logger.LogDebug("User is {Name} from club", clubUser);
            return;
        }

        // Проверка блэклиста
        if (await _userManager.InBanlist(user.Id))
        {
            await _userBanService.BanBlacklistedUserAsync(userJoinMessage, user, cancellationToken);
            return;
        }

        // Проверяем, не находится ли пользователь уже в процессе прохождения капчи
        var captchaKey = _captchaService.GenerateKey(chat.Id, user.Id);
        if (_captchaService.GetCaptchaInfo(captchaKey) != null)
        {
            _logger.LogDebug("Пользователь уже проходит капчу");
            return;
        }

        // Создаем капчу
        var request = new CreateCaptchaRequest(chat, user, userJoinMessage);
        var captchaInfo = await _captchaService.CreateCaptchaAsync(request);
        if (captchaInfo == null)
        {
            _logger.LogInformation($"[NO_CAPTCHA] Капча не требуется для чата {chat.Id}");
            return;
        }
    }

    public async Task HandleChannelMessageAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🔍 MessageHandler: Делегируем обработку канала к ChannelModerationService");
        await _channelModerationService.HandleChannelMessageAsync(message, cancellationToken);
    }

    internal async Task HandleUserMessageAsync(Message message, bool isSilentMode, CancellationToken cancellationToken)
    {
        var user = message.From;
        var chat = message.Chat;

        // Игнорируем сообщения без пользователя (системные сообщения)
        if (user == null)
        {
            _logger.LogDebug("Игнорируем системное сообщение без пользователя");
            return;
        }

        // Игнорируем сообщения от ботов
        if (user.IsBot)
        {
            _logger.LogDebug("Игнорируем сообщение от бота {BotId}", user.Id);
            return;
        }

        // Игнорируем системные сообщения (выход пользователей и т.д.)
        if (message.LeftChatMember != null)
        {
            _logger.LogDebug("Игнорируем системное сообщение о выходе пользователя");
            return;
        }

        // Проверяем, не находится ли пользователь в процессе прохождения капчи
        var captchaKey = _captchaService.GenerateKey(chat.Id, user.Id);
        if (_captchaService.GetCaptchaInfo(captchaKey) != null)
        {
            // Удаляем сообщение от пользователя, который должен пройти капчу
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить сообщение от пользователя проходящего капчу");
            }
            return;
        }

        // ПРИОРИТЕТНАЯ проверка блэклиста lols.bot (выполняется даже для одобренных)
        _logger.LogDebug("🔍 Проверяем пользователя {UserId} по блэклисту lols.bot", user.Id);
        if (await _userManager.InBanlist(user.Id))
        {
            await _userBanService.HandleBlacklistBanAsync(message, user, chat, cancellationToken);
            return;
        }
        _logger.LogDebug("✅ Пользователь {UserId} не найден в блэклисте", user.Id);

        // Проверяем, одобрен ли пользователь
        if (_moderationService.IsUserApproved(user.Id, chat.Id))
        {
            _logger.LogDebug("✅ Пользователь {UserId} уже одобрен в чате {ChatId}, пропускаем модерацию", user.Id, chat.Id);
            return;
        }

        // Логируем сообщения от неодобренных пользователей для анализа
        var messageText = message.Text ?? message.Caption ?? "[медиа/стикер/файл]";
        _userFlowLogger.LogFirstMessage(user, chat, messageText);

        // Определяем тип пользователя
        var isChannelDiscussion = await IsChannelDiscussion(chat, message);
        var userType = isChannelDiscussion ? "из обсуждения канала" : "новый участник";
        
        _logger.LogInformation("==================== СООБЩЕНИЕ ОТ НЕОДОБРЕННОГО ====================\n" +
            "{UserType}: {User} (id={UserId}, username={Username}) в '{ChatTitle}' (id={ChatId})\n" +
            "Сообщение: {Text}\n" +
            "================================================================", 
            userType, Utils.FullName(user), user.Id, user.Username ?? "-", chat.Title ?? "-", chat.Id, 
            (message.Text ?? message.Caption)?.Substring(0, Math.Min((message.Text ?? message.Caption)?.Length ?? 0, 100)) ?? "[медиа]");

        // Проверка на пересланные сообщения от новичков
        if (Config.DeleteForwardedMessages && message.ForwardOrigin != null)
        {
            _logger.LogInformation("🔄 Удаление пересланного сообщения от новичка {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
                Utils.FullName(user), user.Id, chat.Title ?? "-", chat.Id);
            
            // ФИКС: Сначала отправляем предупреждение как реплай на сообщение
            Message? notificationMessage = null;
            try
            {
                // Отправляем предупреждение пользователю как реплай на сообщение
                notificationMessage = await _messageService.SendUserNotificationWithReplyAsync(user, chat, UserNotificationType.MessageDeleted, 
                    new SimpleNotificationData(user, chat, "пересланные сообщения от новичков не разрешены"), 
                    new ReplyParameters { MessageId = message.MessageId },
                    cancellationToken);
                
                // Удаляем уведомление через 10 секунд
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                        await _bot.DeleteMessage(chat.Id, notificationMessage.MessageId, cancellationToken);
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
            
            // ФИКС: Теперь удаляем сообщение ПОСЛЕ отправки предупреждения
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить пересланное сообщение от новичка");
            }
            return;
        }

        // Проверка на клубного пользователя
        var clubName = await _userManager.GetClubUsername(user.Id);
        if (!string.IsNullOrEmpty(clubName))
        {
            _logger.LogDebug("User is {Name} from club", clubName);
            return;
        }

        // Модерация сообщения
        _userFlowLogger.LogModerationStarted(user, chat, messageText);
        ModerationResult moderationResult;
        try
        {
            moderationResult = await _moderationService.CheckMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при модерации сообщения");
            // При ошибке в ModerationService передаем на ручной анализ вместо автоматического разрешения
            // Изначальное поведение: исключение прерывало всю обработку, сообщение не обрабатывалось
            // Новое поведение: передаем на fallback-механизм (RequireManualReview) для безопасности
            moderationResult = new ModerationResult(ModerationAction.RequireManualReview, "Ошибка модерации - требуется ручной анализ", 0);
        }
        _userFlowLogger.LogModerationResult(user, chat, moderationResult.Action.ToString(), moderationResult.Reason, moderationResult.Confidence);
        
        // AI анализ профиля при первом сообщении (после базовой модерации)
        // Выполняем только если базовая модерация разрешила сообщение
        if (moderationResult.Action == ModerationAction.Allow)
        {
            var profileAnalysisResult = await PerformAiProfileAnalysis(message, user, chat, cancellationToken);
            if (profileAnalysisResult)
            {
                // Пользователь получил ограничения за подозрительный профиль, возвращаемся
                return;
            }
        }
        
        switch (moderationResult.Action)
        {
            case ModerationAction.Allow:
                _logger.LogDebug("Сообщение разрешено: {Reason}", moderationResult.Reason);
                var allowedMessageText = message.Text ?? message.Caption ?? "";
                
                // Проверяем AI детект для подозрительных пользователей
                var aiDetectBlocked = await _moderationService.CheckAiDetectAndNotifyAdminsAsync(user, chat, message);
                
                // Засчитываем хорошее сообщение только если пользователь не был заблокирован AI детектом
                if (!aiDetectBlocked)
                {
                    await _moderationService.IncrementGoodMessageCountAsync(user, chat, allowedMessageText);
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

    private async Task<bool> IsChannelDiscussion(Chat chat, Message message)
    {
        // WRAP: delegated to ForwardingService
        return await _forwardingService.IsChannelDiscussion(chat, message);
    }

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

    public async Task SendSuspiciousMessageWithButtons(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken)
    {
        // WRAP: delegated to ButtonsService
        await _buttonsService.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, cancellationToken);
    }

    private async Task<bool> PerformAiProfileAnalysis(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        // WRAP: delegated to AiCascadeService
        return await _aiCascadeService.PerformAiProfileAnalysisAsync(message, user, chat, cancellationToken);
    }

    internal async Task HandleAiCascadeAnalysis(Message message, User user, double mlScore, bool isSilentMode, CancellationToken cancellationToken)
    {
        // WRAP: delegated to AiCascadeService
        await _aiCascadeService.HandleAiCascadeAnalysisAsync(message, user, mlScore, isSilentMode, cancellationToken);
    }

    #region IMessageHandler Implementation

    /// <summary>
    /// Определяет, может ли данный обработчик обработать указанное сообщение
    /// </summary>
    public bool CanHandle(Message message)
    {
        var update = new Update { Message = message };
        return CanHandle(update);
    }

    /// <summary>
    /// Обрабатывает сообщение
    /// </summary>
    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var update = new Update { Message = message };
        await HandleAsync(update, cancellationToken);
    }

    #endregion

    public void DeleteMessageLater(Message message, TimeSpan after = default, CancellationToken cancellationToken = default)
    {
        if (message == null) return;
        if (after == default) after = TimeSpan.FromMinutes(5);
        _ = Task.Run(
            async () =>
            {
                try
                {
                    if (after > TimeSpan.Zero)
                        await Task.Delay(after, cancellationToken);
                    await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessageLater failed");
                }
            },
            cancellationToken);
    }
}