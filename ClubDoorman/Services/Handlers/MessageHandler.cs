using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Text;
using Microsoft.Extensions.Logging;
using ClubDoorman.Features.AdminOps;
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
using ClubDoorman.Features.UserJoin;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Services.Notifications;

namespace ClubDoorman.Services.Handlers;

/// <summary>
/// Обработчик сообщений
/// </summary>
public class MessageHandler : IUpdateHandler, IMessageHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IUserManager _userManager;
    private readonly IAppConfig _appConfig;
    private readonly IUserBanService _userBanService;
    private readonly IChannelModerationService _channelModerationService;
    private readonly ICommandRouter _commandRouter;
    private readonly IAiCascadeService _aiCascadeService; // injected service
    private readonly IUserJoinFacade _userJoinFacade; // injected service
    private readonly IModerationFacade _moderationFacade; // injected service
    private readonly ILogger<MessageHandler> _logger;
    private readonly IBotPermissionsService _botPermissionsService;
    private readonly ICaptchaService _captchaService;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IForwardingService _forwardingService;
    private readonly IMessageService _messageService;
    private readonly IButtonsService _buttonsService;

    /// <summary>
    /// Создает экземпляр обработчика сообщений.
    /// </summary>
    /// <param name="bot">Клиент Telegram бота</param>
    /// <param name="userManager">Менеджер пользователей</param>
    /// <param name="appConfig">Конфигурация приложения</param>
    /// <param name="userBanService">Сервис управления банами пользователей</param>
    /// <param name="channelModerationService">Сервис модерации каналов</param>
    /// <param name="commandRouter">Маршрутизатор команд</param>
    /// <param name="aiCascadeService">Сервис каскадного AI</param>
    /// <param name="userJoinFacade">Фасад для управления присоединением пользователей</param>
    /// <param name="moderationFacade">Фасад для модерации</param>
    /// <param name="logger">Логгер</param>
    /// <param name="botPermissionsService">Сервис проверки прав бота</param>
    /// <param name="captchaService">Сервис капчи</param>
    /// <param name="userFlowLogger">Логгер потока пользователей</param>
    /// <param name="forwardingService">Сервис пересылки</param>
    /// <param name="messageService">Сервис сообщений</param>
    /// <param name="buttonsService">Сервис кнопок</param>
    /// <exception cref="ArgumentNullException">Если любой из параметров равен null</exception>
    public MessageHandler(
        ITelegramBotClientWrapper bot,
        IUserManager userManager,
        IAppConfig appConfig,
        IUserBanService userBanService,
        IChannelModerationService channelModerationService,
        ICommandRouter commandRouter,
        IAiCascadeService aiCascadeService,
        IUserJoinFacade userJoinFacade,
        IModerationFacade moderationFacade,
        ILogger<MessageHandler> logger,
        IBotPermissionsService botPermissionsService,
        ICaptchaService captchaService,
        IUserFlowLogger userFlowLogger,
        IForwardingService forwardingService,
        IMessageService messageService,
        IButtonsService buttonsService)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _userBanService = userBanService ?? throw new ArgumentNullException(nameof(userBanService));
        _channelModerationService = channelModerationService ?? throw new ArgumentNullException(nameof(channelModerationService));
        _commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
        _aiCascadeService = aiCascadeService ?? throw new ArgumentNullException(nameof(aiCascadeService));
        _userJoinFacade = userJoinFacade ?? throw new ArgumentNullException(nameof(userJoinFacade));
        _moderationFacade = moderationFacade ?? throw new ArgumentNullException(nameof(moderationFacade));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _botPermissionsService = botPermissionsService ?? throw new ArgumentNullException(nameof(botPermissionsService));
        _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        _userFlowLogger = userFlowLogger ?? throw new ArgumentNullException(nameof(userFlowLogger));
        _forwardingService = forwardingService ?? throw new ArgumentNullException(nameof(forwardingService));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
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

    public async Task HandleNewMembersAsync(Message message, CancellationToken cancellationToken)
    {
        // WRAP: delegated to UserJoinFacade
        await _userJoinFacade.HandleNewMembersAsync(message, cancellationToken);
    }

    public async Task ProcessNewUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        // WRAP: delegated to UserJoinFacade
        await _userJoinFacade.ProcessNewUserAsync(userJoinMessage, user, cancellationToken);
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
        if (_moderationFacade.IsUserApproved(user.Id, chat.Id))
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
            moderationResult = await _moderationFacade.CheckMessageAsync(message);
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
        
        await _moderationFacade.HandleUserMessageAsync(message, user, chat, moderationResult, isSilentMode, cancellationToken);
        return;
    }

    private async Task<bool> IsChannelDiscussion(Chat chat, Message message)
    {
        // WRAP: delegated to ForwardingService
        return await _forwardingService.IsChannelDiscussion(chat, message);
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