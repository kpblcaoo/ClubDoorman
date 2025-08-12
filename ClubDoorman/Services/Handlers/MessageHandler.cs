using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.UserBan;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Features.UserJoin;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Services.Notifications;

namespace ClubDoorman.Services.Handlers;

/// <summary>
/// Обработчик сообщений
/// </summary>
public class MessageHandler : IUpdateHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IUserManager _userManager;
    private readonly IAppConfig _appConfig;
    private readonly IUserBanService _userBanService;
    private readonly IChannelModerationService _channelModerationService;
    private readonly ICommandRouter _commandRouter;
    private readonly IUserJoinFacade _userJoinFacade; // injected service
    private readonly IModerationFacade _moderationFacade; // injected service
    private readonly ILogger<MessageHandler> _logger;
    private readonly IBotPermissionsService _botPermissionsService;
    private readonly ICaptchaService _captchaService;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IForwardingService _forwardingService;

    /// <summary>
    /// Создает экземпляр обработчика сообщений.
    /// </summary>
    /// <param name="bot">Клиент Telegram бота</param>
    /// <param name="userManager">Менеджер пользователей</param>
    /// <param name="appConfig">Конфигурация приложения</param>
    /// <param name="userBanService">Сервис управления банами пользователей</param>
    /// <param name="channelModerationService">Сервис модерации каналов</param>
    /// <param name="commandRouter">Маршрутизатор команд</param>
    /// <param name="userJoinFacade">Фасад для управления присоединением пользователей</param>
    /// <param name="moderationFacade">Фасад для модерации</param>
    /// <param name="logger">Логгер</param>
    /// <param name="botPermissionsService">Сервис проверки прав бота</param>
    /// <param name="captchaService">Сервис капчи</param>
    /// <param name="userFlowLogger">Логгер потока пользователей</param>
    /// <param name="forwardingService">Сервис пересылки</param>
    /// <exception cref="ArgumentNullException">Если любой из параметров равен null</exception>
    public MessageHandler(
        ITelegramBotClientWrapper bot,
        IUserManager userManager,
        IAppConfig appConfig,
        IUserBanService userBanService,
        IChannelModerationService channelModerationService,
        ICommandRouter commandRouter,
        IUserJoinFacade userJoinFacade,
        IModerationFacade moderationFacade,
        ILogger<MessageHandler> logger,
        IBotPermissionsService botPermissionsService,
        ICaptchaService captchaService,
        IUserFlowLogger userFlowLogger,
        IForwardingService forwardingService)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _userBanService = userBanService ?? throw new ArgumentNullException(nameof(userBanService));
        _channelModerationService = channelModerationService ?? throw new ArgumentNullException(nameof(channelModerationService));
        _commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
        _userJoinFacade = userJoinFacade ?? throw new ArgumentNullException(nameof(userJoinFacade));
        _moderationFacade = moderationFacade ?? throw new ArgumentNullException(nameof(moderationFacade));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _botPermissionsService = botPermissionsService ?? throw new ArgumentNullException(nameof(botPermissionsService));
        _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        _userFlowLogger = userFlowLogger ?? throw new ArgumentNullException(nameof(userFlowLogger));
        _forwardingService = forwardingService ?? throw new ArgumentNullException(nameof(forwardingService));
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
        using (_logger.BeginScope(new Dictionary<string, object>{{"opId", Guid.NewGuid()}}))
        {
            try
            {
                _logger.LogTrace("[TRACE] HandleAsync started");
                if (update == null) throw new ArgumentNullException(nameof(update));
                if (update.Message == null && update.EditedMessage == null) 
                    throw new ArgumentNullException(nameof(update.Message));

                var message = update.EditedMessage ?? update.Message!;
                var chat = message.Chat;

                _logger.LogTrace("[TRACE] Received message '{MessageText}' in chat {ChatId}", message.Text, chat.Id);

                // Проверка whitelist - если активен, работаем только в разрешённых чатах
                // ИСКЛЮЧЕНИЕ: админ-чаты всегда обрабатываются (для команд /spam, /ham и т.д.)
                var isAdminChat = chat.Id == _appConfig.AdminChatId || chat.Id == _appConfig.LogAdminChatId;
                
                if (!_appConfig.IsChatAllowed(chat.Id) && !isAdminChat)
                {
                    _logger.LogTrace("[TRACE] Chat {ChatId} not in whitelist, skipping", chat.Id);
                    _logger.LogDebug("Чат {ChatId} ({ChatTitle}) не в whitelist - игнорируем", chat.Id, chat.Title);
                    return;
                }

                // Игнорировать полностью отключённые чаты
                if (_appConfig.DisabledChats.Contains(chat.Id)) {
                    _logger.LogTrace("[TRACE] Chat {ChatId} is disabled, skipping", chat.Id);
                    return;
                }

                // Проверяем тихий режим (бот без прав администратора)
                var isSilentMode = await _botPermissionsService.IsSilentModeAsync(chat.Id, cancellationToken);
                if (isSilentMode)
                {
                    _logger.LogTrace("[TRACE] Silent mode detected for chat {ChatId}", chat.Id);
                    _logger.LogInformation("🔇 Тихий режим в чате {ChatId} ({ChatTitle}) - бот без прав администратора", chat.Id, chat.Title);
                }

                // Автоматически добавляем чат в конфиг
                _logger.LogTrace("[TRACE] Ensuring chat {ChatId} is in config", chat.Id);
                ChatSettingsManager.EnsureChatInConfig(chat.Id, chat.Title);

                // Обработка команд
                if (message.Text?.StartsWith("/") == true)
                {
                    _logger.LogTrace("[TRACE] Handling command '{Command}'", message.Text);
                    await HandleCommandAsync(message, cancellationToken);
                    return;
                }

                _logger.LogTrace("[TRACE] Not a command, continuing regular processing");

                // Для приватных чатов обрабатываем только команды, остальное игнорируем
                if (chat.Type == ChatType.Private)
                {
                    _logger.LogTrace("[TRACE] Private chat {ChatId}, only commands processed", chat.Id);
                    _logger.LogDebug("Приватный чат {ChatId} - обрабатываем только команды", chat.Id);
                    return;
                }

                // Обработка новых участников
                if (message.NewChatMembers != null && chat.Id != _appConfig.AdminChatId)
                {
                    _logger.LogTrace("[TRACE] Handling new chat members in chat {ChatId}", chat.Id);
                    await _userJoinFacade.HandleNewMembersAsync(message, cancellationToken);
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
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "[CRITICAL] Unhandled exception in MessageHandler.HandleAsync");
                throw;
            }
        }
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

    // Удалить метод TryFindUserIdByUsername и все обращения к _userIndex

 //   public async Task HandleNewMembersAsync delegated to UserJoinFacade


 //   public async Task ProcessNewUserAsync delegated to UserJoinFacade

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
        if (_moderationFacade.IsUserApproved(user.Id, chat.Id))
        {
            _logger.LogDebug("✅ Пользователь {UserId} уже одобрен в чате {ChatId}, пропускаем модерацию", user.Id, chat.Id);
            return;
        }

        // Логируем сообщения от неодобренных пользователей для анализа
        var messageText = message.Text ?? message.Caption ?? "[медиа/стикер/файл]";
        _userFlowLogger.LogFirstMessage(user, chat, messageText);

        // Определяем тип пользователя
        var isChannelDiscussion = await _forwardingService.IsChannelDiscussion(chat, message);
        var userType = isChannelDiscussion ? "из обсуждения канала" : "новый участник";
        
        _logger.LogInformation("==================== СООБЩЕНИЕ ОТ НЕОДОБРЕННОГО ====================\n" +
            "{UserType}: {User} (id={UserId}, username={Username}) в '{ChatTitle}' (id={ChatId})\n" +
            "Сообщение: {Text}\n" +
            "================================================================", 
            userType, Utils.FullName(user), user.Id, user.Username ?? "-", chat.Title ?? "-", chat.Id, 
            (message.Text ?? message.Caption)?.Substring(0, Math.Min((message.Text ?? message.Caption)?.Length ?? 0, 100)) ?? "[медиа]");

        // Проверка на клубного пользователя
        var clubName = await _userManager.GetClubUsername(user.Id);
        if (!string.IsNullOrEmpty(clubName))
        {
            _logger.LogDebug("User is {Name} from club", clubName);
            return;
        }

        // Вся модерационная логика теперь делегируется фасаду
        ModerationResult moderationResult;
        try
        {
            moderationResult = await _moderationFacade.CheckMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при модерации сообщения");
            moderationResult = new ModerationResult(ModerationAction.RequireManualReview, "Ошибка модерации - требуется ручной анализ", 0);
        }
        _userFlowLogger.LogModerationResult(user, chat, moderationResult.Action.ToString(), moderationResult.Reason, moderationResult.Confidence);

        await _moderationFacade.HandleUserMessageAsync(message, user, chat, moderationResult, isSilentMode, cancellationToken);
        return;
    }




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