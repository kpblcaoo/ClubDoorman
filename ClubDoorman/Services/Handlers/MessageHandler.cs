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
using ClubDoorman.Services.AI;

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
    private readonly IAiCascadeService _aiCascadeService;

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
        IForwardingService forwardingService,
        IAiCascadeService aiCascadeService)
    {
        _logger?.LogDebug("MessageHandler constructor called");
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
        _aiCascadeService = aiCascadeService ?? throw new ArgumentNullException(nameof(aiCascadeService));
        _logger.LogDebug("MessageHandler constructed successfully");
    }

    public bool CanHandle(Update update)
    {
        _logger.LogTrace("CanHandle called. Update is null: {IsNull}, Has Message: {HasMessage}, Has EditedMessage: {HasEditedMessage}",
            update == null, update?.Message != null, update?.EditedMessage != null);
        bool canHandle = update?.Message != null || update?.EditedMessage != null;
        _logger.LogTrace("CanHandle result: {CanHandle}", canHandle);
        return canHandle;
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("HandleAsync called");
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["opId"] = Guid.NewGuid(),
            ["updateType"] = update?.Type.ToString() ?? "null"
        });
        
        try
        {
            if (update == null)
            {
                _logger.LogError("HandleAsync: update is null");
                throw new ArgumentNullException(nameof(update));
            }
            if (update.Message == null && update.EditedMessage == null)
            {
                _logger.LogError("HandleAsync: update.Message and update.EditedMessage are null");
                throw new ArgumentNullException(nameof(update.Message));
            }

            var message = update.EditedMessage ?? update.Message!;
            var chat = message.Chat;

            _logger.LogDebug("Received message '{MessageText}' in chat {ChatId} (type: {ChatType}, title: {ChatTitle})", 
                message.Text, chat.Id, chat.Type, chat.Title);

            var isAdminChat = chat.Id == _appConfig.AdminChatId || chat.Id == _appConfig.LogAdminChatId;
            _logger.LogDebug("Checking whitelist for chat {ChatId}. IsAdminChat: {IsAdminChat}, IsAllowed: {IsAllowed}", 
                chat.Id, isAdminChat, _appConfig.IsChatAllowed(chat.Id));

            if (!_appConfig.IsChatAllowed(chat.Id) && !isAdminChat)
            {
                _logger.LogDebug("Chat {ChatId} not in whitelist, skipping", chat.Id);
                _logger.LogInformation("HandleAsync: Chat {ChatId} is not allowed, returning", chat.Id);
                return;
            }

            if (_appConfig.DisabledChats.Contains(chat.Id)) {
                _logger.LogDebug("Chat {ChatId} is disabled, skipping", chat.Id);
                _logger.LogInformation("HandleAsync: Chat {ChatId} is disabled, returning", chat.Id);
                return;
            }

            _logger.LogTrace("Checking silent mode for chat {ChatId}", chat.Id);
            var isSilentMode = await _botPermissionsService.IsSilentModeAsync(chat.Id, cancellationToken);
            _logger.LogTrace("Silent mode for chat {ChatId}: {IsSilentMode}", chat.Id, isSilentMode);
            if (isSilentMode)
            {
                _logger.LogDebug("Silent mode detected for chat {ChatId}", chat.Id);
                _logger.LogInformation("🔇 Тихий режим в чате {ChatId} ({ChatTitle}) - бот без прав администратора", chat.Id, chat.Title);
            }

            ChatSettingsManager.EnsureChatInConfig(chat.Id, chat.Title);

            if (message.Text?.StartsWith("/") == true)
            {
                _logger.LogDebug("Handling command '{Command}' in chat {ChatId}", message.Text, chat.Id);
                await HandleCommandAsync(message, cancellationToken);
                _logger.LogDebug("HandleAsync: Command handled, returning");
                return;
            }

            _logger.LogTrace("Not a command, continuing regular processing for chat {ChatId}", chat.Id);

            if (chat.Type == ChatType.Private)
            {
                _logger.LogDebug("Private chat {ChatId}, only commands processed", chat.Id);
                _logger.LogInformation("HandleAsync: Private chat, only commands processed, returning");
                return;
            }

            if (message.NewChatMembers != null && chat.Id != _appConfig.AdminChatId)
            {
                _logger.LogDebug("Handling new chat members in chat {ChatId}. Members: {Members}", 
                    chat.Id, string.Join(", ", message.NewChatMembers.Select(m => $"{m.Id} ({m.Username})")));
                await _userJoinFacade.HandleNewMembersAsync(message, cancellationToken);
                _logger.LogDebug("HandleAsync: New chat members handled, returning");
                return;
            }

            if (message.LeftChatMember != null && message.From?.Id == _bot.BotId)
            {
                _logger.LogDebug("Message about left chat member detected. MessageId: {MessageId}, UserId: {UserId}", 
                    message.MessageId, message.LeftChatMember.Id);
                try
                {
                    await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                    _logger.LogDebug("Удалено сообщение о бане/исключении пользователя (UserId: {UserId})", message.LeftChatMember.Id);
                    _logger.LogInformation("HandleAsync: Deleted left chat member message. MessageId: {MessageId}", message.MessageId);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Не удалось удалить сообщение о бане/исключении (UserId: {UserId})", message.LeftChatMember.Id);
                    _logger.LogError(e, "HandleAsync: Exception while deleting left chat member message. MessageId: {MessageId}", message.MessageId);
                }
                return;
            }

            if (message.SenderChat != null)
            {
                _logger.LogDebug("Message from channel detected. SenderChatId: {SenderChatId}, Title: {SenderChatTitle}", 
                    message.SenderChat.Id, message.SenderChat.Title);
                await HandleChannelMessageAsync(message, cancellationToken);
                _logger.LogDebug("HandleAsync: Channel message handled, returning");
                return;
            }

            _logger.LogDebug("Processing user message in chat {ChatId}, messageId: {MessageId}", chat.Id, message.MessageId);
            await HandleUserMessageAsync(message, isSilentMode, cancellationToken);
            _logger.LogDebug("HandleAsync: User message handled, returning");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[CRITICAL] Unhandled exception in MessageHandler.HandleAsync");
            throw;
        }
    }

    public async Task HandleCommandAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("HandleCommandAsync called. MessageId: {MessageId}, ChatId: {ChatId}, Text: {Text}", 
            message.MessageId, message.Chat.Id, message.Text);

        // Обрабатываем команду через CommandRouter
        _logger.LogTrace("HandleCommandAsync: Passing command to CommandRouter. MessageId: {MessageId}, ChatId: {ChatId}", 
            message.MessageId, message.Chat.Id);
        var handled = await _commandRouter.HandleCommandAsync(message, cancellationToken);

        _logger.LogDebug("CommandRouter.HandleCommandAsync returned {Handled} for command '{Command}' in chat {ChatId}", 
            handled, message.Text, message.Chat.Id);

        if (handled)
        {
            _logger.LogDebug("Команда обработана через CommandRouter: {Command} (chatId: {ChatId}, messageId: {MessageId})",
                message.Text?.Split(' ')[0], message.Chat.Id, message.MessageId);
            _logger.LogInformation("HandleCommandAsync: Command '{Command}' handled successfully. ChatId: {ChatId}, MessageId: {MessageId}", 
                message.Text, message.Chat.Id, message.MessageId);
        }
        else
        {
            _logger.LogDebug("CommandRouter не смог обработать команду: {Command} (chatId: {ChatId}, messageId: {MessageId})",
                message.Text?.Split(' ')[0], message.Chat.Id, message.MessageId);
            _logger.LogWarning("HandleCommandAsync: Command '{Command}' was not handled. ChatId: {ChatId}, MessageId: {MessageId}", 
                message.Text, message.Chat.Id, message.MessageId);
        }
    }

    public async Task HandleChannelMessageAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("HandleChannelMessageAsync called. MessageId: {MessageId}, SenderChatId: {SenderChatId}, ChatId: {ChatId}", 
            message.MessageId, message.SenderChat?.Id, message.Chat.Id);
        _logger.LogDebug("🔍 MessageHandler: Делегируем обработку канала к ChannelModerationService. MessageId: {MessageId}, SenderChatId: {SenderChatId}, ChatId: {ChatId}", 
            message.MessageId, message.SenderChat?.Id, message.Chat.Id);
        await _channelModerationService.HandleChannelMessageAsync(message, cancellationToken);
        _logger.LogDebug("HandleChannelMessageAsync: Channel message processed. MessageId: {MessageId}", message.MessageId);
    }

    internal async Task HandleUserMessageAsync(Message message, bool isSilentMode, CancellationToken cancellationToken)
    {
        _logger.LogDebug("HandleUserMessageAsync called. MessageId: {MessageId}, IsSilentMode: {IsSilentMode}", 
            message.MessageId, isSilentMode);
        var user = message.From;
        var chat = message.Chat;

        _logger.LogTrace("HandleUserMessageAsync: user={UserId}, chat={ChatId}", user?.Id, chat.Id);

        if (user == null)
        {
            _logger.LogDebug("Игнорируем системное сообщение без пользователя. MessageId: {MessageId}, ChatId: {ChatId}", 
                message.MessageId, chat.Id);
            _logger.LogInformation("HandleUserMessageAsync: System message without user, returning. MessageId: {MessageId}", message.MessageId);
            return;
        }

        if (user.IsBot)
        {
            _logger.LogDebug("Игнорируем сообщение от бота {BotId}. MessageId: {MessageId}, ChatId: {ChatId}", 
                user.Id, message.MessageId, chat.Id);
            _logger.LogInformation("HandleUserMessageAsync: Message from bot {BotId}, returning. MessageId: {MessageId}", user.Id, message.MessageId);
            return;
        }

        if (message.LeftChatMember != null)
        {
            _logger.LogDebug("Игнорируем системное сообщение о выходе пользователя. MessageId: {MessageId}, LeftUserId: {LeftUserId}, ChatId: {ChatId}", 
                message.MessageId, message.LeftChatMember.Id, chat.Id);
            _logger.LogInformation("HandleUserMessageAsync: System message about left user, returning. MessageId: {MessageId}", message.MessageId);
            return;
        }

        var captchaKey = _captchaService.GenerateKey(chat.Id, user.Id);
        var captchaInfo = _captchaService.GetCaptchaInfo(captchaKey);
        _logger.LogTrace("Captcha check for user {UserId} in chat {ChatId}: {HasCaptcha}", user.Id, chat.Id, captchaInfo != null);
        if (captchaInfo != null)
        {
            _logger.LogInformation("Удаляем сообщение от пользователя {UserId}, который должен пройти капчу. MessageId: {MessageId}, ChatId: {ChatId}", 
                user.Id, message.MessageId, chat.Id);
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                _logger.LogDebug("Сообщение пользователя {UserId} удалено из-за незавершённой капчи. MessageId: {MessageId}, ChatId: {ChatId}", 
                    user.Id, message.MessageId, chat.Id);
                _logger.LogInformation("HandleUserMessageAsync: Deleted message from user {UserId} due to pending captcha. MessageId: {MessageId}", user.Id, message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить сообщение от пользователя проходящего капчу. UserId: {UserId}, MessageId: {MessageId}, ChatId: {ChatId}", 
                    user.Id, message.MessageId, chat.Id);
                _logger.LogError(ex, "HandleUserMessageAsync: Exception while deleting message for captcha. UserId: {UserId}, MessageId: {MessageId}", user.Id, message.MessageId);
            }
            return;
        }

        _logger.LogDebug("🔍 Проверяем пользователя {UserId} по блэклисту lols.bot", user.Id);
        if (await _userManager.InBanlist(user.Id))
        {
            _logger.LogWarning("Пользователь {UserId} найден в блэклисте lols.bot. Применяем бан. ChatId: {ChatId}, MessageId: {MessageId}", 
                user.Id, chat.Id, message.MessageId);
            _logger.LogInformation("HandleUserMessageAsync: User {UserId} found in banlist, banning. MessageId: {MessageId}", user.Id, message.MessageId);
            await _userBanService.HandleBlacklistBanAsync(message, user, chat, cancellationToken);
            return;
        }
        _logger.LogDebug("✅ Пользователь {UserId} не найден в блэклисте", user.Id);
        _logger.LogTrace("HandleUserMessageAsync: User {UserId} not in banlist", user.Id);

        if (_moderationFacade.IsUserApproved(user.Id, chat.Id))
        {
            _logger.LogDebug("✅ Пользователь {UserId} уже одобрен в чате {ChatId}, пропускаем модерацию. MessageId: {MessageId}", 
                user.Id, chat.Id, message.MessageId);
            _logger.LogInformation("HandleUserMessageAsync: User {UserId} already approved in chat {ChatId}, skipping moderation. MessageId: {MessageId}", 
                user.Id, chat.Id, message.MessageId);
            return;
        }

        var messageText = message.Text ?? message.Caption ?? "[медиа/стикер/файл]";
        _logger.LogTrace("Логируем первое сообщение от неодобренного пользователя. UserId: {UserId}, ChatId: {ChatId}, MessageId: {MessageId}, Text: {Text}", 
            user.Id, chat.Id, message.MessageId, messageText);
        _userFlowLogger.LogFirstMessage(user, chat, messageText);

        var isChannelDiscussion = await _forwardingService.IsChannelDiscussion(chat, message);
        var userType = isChannelDiscussion ? "из обсуждения канала" : "новый участник";

        _logger.LogInformation("==================== СООБЩЕНИЕ ОТ НЕОДОБРЕННОГО ====================\n" +
            "{UserType}: {User} (id={UserId}, username={Username}) в '{ChatTitle}' (id={ChatId})\n" +
            "Сообщение: {Text}\n" +
            "================================================================",
            userType, Utils.FullName(user), user.Id, user.Username ?? "-", chat.Title ?? "-", chat.Id,
            (message.Text ?? message.Caption)?.Substring(0, Math.Min((message.Text ?? message.Caption)?.Length ?? 0, 100)) ?? "[медиа]");

        var clubName = await _userManager.GetClubUsername(user.Id);
        _logger.LogTrace("Проверка на клубного пользователя. UserId: {UserId}, ClubName: {ClubName}", user.Id, clubName);
        if (!string.IsNullOrEmpty(clubName))
        {
            _logger.LogDebug("User is {Name} from club. UserId: {UserId}, ChatId: {ChatId}", clubName, user.Id, chat.Id);
            _logger.LogInformation("HandleUserMessageAsync: User {UserId} is a club member ({ClubName}), skipping moderation. MessageId: {MessageId}", 
                user.Id, clubName, message.MessageId);
            return;
        }

        ModerationResult moderationResult;
        try
        {
            _logger.LogTrace("Вызов CheckMessageAsync для модерации сообщения. UserId: {UserId}, ChatId: {ChatId}, MessageId: {MessageId}", 
                user.Id, chat.Id, message.MessageId);
            moderationResult = await _moderationFacade.CheckMessageAsync(message);
            _logger.LogDebug("Результат модерации: Action={Action}, Reason={Reason}, Confidence={Confidence}", 
                moderationResult.Action, moderationResult.Reason, moderationResult.Confidence);
            _logger.LogInformation("HandleUserMessageAsync: Moderation result: Action={Action}, Reason={Reason}, Confidence={Confidence}", 
                moderationResult.Action, moderationResult.Reason, moderationResult.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при модерации сообщения. UserId: {UserId}, ChatId: {ChatId}, MessageId: {MessageId}", 
                user.Id, chat.Id, message.MessageId);
            _logger.LogCritical(ex, "HandleUserMessageAsync: Exception during moderation. UserId: {UserId}, MessageId: {MessageId}", user.Id, message.MessageId);
            moderationResult = new ModerationResult(ModerationAction.RequireManualReview, "Ошибка модерации - требуется ручной анализ", 0);
        }
        _userFlowLogger.LogModerationResult(user, chat, moderationResult.Action.ToString(), moderationResult.Reason, moderationResult.Confidence);

        if (moderationResult.Action == ModerationAction.Allow)
        {
            _logger.LogTrace("AI анализ профиля для пользователя {UserId} после успешной базовой модерации. ChatId: {ChatId}, MessageId: {MessageId}", 
                user.Id, chat.Id, message.MessageId);
            var profileAnalysisResult = await _aiCascadeService.PerformAiProfileAnalysisAsync(message, user, chat, cancellationToken);
            _logger.LogDebug("Результат AI анализа профиля: {ProfileAnalysisResult} (UserId: {UserId}, ChatId: {ChatId})", 
                profileAnalysisResult, user.Id, chat.Id);
            _logger.LogInformation("HandleUserMessageAsync: AI profile analysis result: {ProfileAnalysisResult} (UserId: {UserId}, ChatId: {ChatId})", 
                profileAnalysisResult, user.Id, chat.Id);
            if (profileAnalysisResult)
            {
                _logger.LogWarning("Пользователь {UserId} получил ограничения за подозрительный профиль. ChatId: {ChatId}, MessageId: {MessageId}", 
                    user.Id, chat.Id, message.MessageId);
                _logger.LogInformation("HandleUserMessageAsync: User {UserId} restricted due to suspicious profile. MessageId: {MessageId}", user.Id, message.MessageId);
                return;
            }
        }

        _logger.LogTrace("Передаём сообщение на финальную обработку в ModerationFacade. UserId: {UserId}, ChatId: {ChatId}, MessageId: {MessageId}, Action: {Action}", 
            user.Id, chat.Id, message.MessageId, moderationResult.Action);
        await _moderationFacade.HandleUserMessageAsync(message, user, chat, moderationResult, isSilentMode, cancellationToken);
        _logger.LogTrace("HandleUserMessageAsync: Message passed to ModerationFacade. UserId: {UserId}, MessageId: {MessageId}", user.Id, message.MessageId);
        return;
    }

    public void DeleteMessageLater(Message message, TimeSpan after = default, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("DeleteMessageLater called. MessageId: {MessageId}, ChatId: {ChatId}, After: {After}", 
            message?.MessageId, message?.Chat?.Id, after);
        if (message == null)
        {
            _logger.LogWarning("DeleteMessageLater: message is null, skipping");
            return;
        }
        if (after == default) after = TimeSpan.FromMinutes(5);
        _logger.LogDebug("Запланировано удаление сообщения {MessageId} в чате {ChatId} через {After}", 
            message.MessageId, message.Chat.Id, after);
        _ = Task.Run(
            async () =>
            {
                try
                {
                    if (after > TimeSpan.Zero)
                    {
                        _logger.LogTrace("Ожидание {After} перед удалением сообщения {MessageId} в чате {ChatId}", 
                            after, message.MessageId, message.Chat.Id);
                        await Task.Delay(after, cancellationToken);
                    }
                    _logger.LogTrace("Удаляем сообщение {MessageId} в чате {ChatId}", message.MessageId, message.Chat.Id);
                    await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                    _logger.LogDebug("Сообщение {MessageId} в чате {ChatId} успешно удалено", message.MessageId, message.Chat.Id);
                    _logger.LogInformation("DeleteMessageLater: Message {MessageId} in chat {ChatId} deleted", message.MessageId, message.Chat.Id);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessageLater failed for message {MessageId} in chat {ChatId}", message.MessageId, message.Chat.Id);
                    _logger.LogError(ex, "DeleteMessageLater: Exception while deleting message {MessageId} in chat {ChatId}", message.MessageId, message.Chat.Id);
                }
            },
            cancellationToken);
    }
}