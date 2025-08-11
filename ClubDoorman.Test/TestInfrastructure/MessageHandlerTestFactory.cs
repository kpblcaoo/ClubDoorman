using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.Commands;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Handlers;

using ClubDoorman.Services;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Test.TestKit;
using ClubDoorman.Test.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Notifications;


namespace ClubDoorman.Test.TestInfrastructure;

/// <summary>
/// Фабрика для создания MessageHandler с настроенными моками
/// Использует TestKit для создания моков и тестовых данных
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class MessageHandlerTestFactory
{
    // Используем TestKit для создания моков
    public Mock<ITelegramBotClientWrapper> BotMock { get; } = TK.CreateMockBotClientWrapper();
    public Mock<IModerationService> ModerationServiceMock { get; } = TK.CreateMockModerationService();
    public Mock<ICaptchaService> CaptchaServiceMock { get; } = TK.CreateMockCaptchaService();
    public Mock<IUserManager> UserManagerMock { get; } = TK.CreateMockUserManager();
    public Mock<ISpamHamClassifier> ClassifierMock { get; } = TK.CreateMockSpamHamClassifier();
    public Mock<IBadMessageManager> BadMessageManagerMock { get; } = TK.CreateMock<IBadMessageManager>();
    public Mock<IAiChecks> AiChecksMock { get; } = TK.CreateMockAiChecks();
    public Mock<IStatisticsService> StatisticsServiceMock { get; } = TK.CreateMockStatisticsService();
    public Mock<IServiceProvider> ServiceProviderMock { get; } = TK.CreateMockServiceProvider();
    public Mock<IUserFlowLogger> UserFlowLoggerMock { get; } = TK.CreateMock<IUserFlowLogger>();
    public Mock<IMessageService> MessageServiceMock { get; } = TK.CreateMockMessageService();
    public Mock<IChatLinkFormatter> ChatLinkFormatterMock { get; } = TK.CreateMock<IChatLinkFormatter>();
    public Mock<IBotPermissionsService> BotPermissionsServiceMock { get; } = TK.CreateMockBotPermissionsService();
    public Mock<IAppConfig> AppConfigMock { get; } = TK.CreateMockAppConfig();
    public Mock<IViolationTracker> ViolationTrackerMock { get; } = TK.CreateMockViolationTracker();
    public Mock<IUserBanService> UserBanServiceMock { get; } = TK.CreateMockUserBanService();
    public Mock<IChannelModerationService> ChannelModerationServiceMock { get; } = TK.CreateMock<IChannelModerationService>();
    public Mock<ILogChatService> LogChatServiceMock { get; } = TK.CreateMock<ILogChatService>();
    public Mock<IAiCascadeService> AiCascadeServiceMock { get; } = TK.CreateMock<IAiCascadeService>();
    public Mock<INotificationService> NotificationServiceMock { get; } = TK.CreateMock<INotificationService>();
    public Mock<ClubDoorman.Services.Notifications.IForwardingService> ForwardingServiceMock { get; } = TK.CreateMock<ClubDoorman.Services.Notifications.IForwardingService>();
    public Mock<ClubDoorman.Services.Notifications.IButtonsService> ButtonsServiceMock { get; } = TK.CreateMock<ClubDoorman.Services.Notifications.IButtonsService>();

    public Mock<IUserCleanupService> UserCleanupServiceMock { get; } = TK.CreateMock<IUserCleanupService>();
    public Mock<IJoinedUserFlags> JoinedUserFlagsMock { get; } = TK.CreateMock<IJoinedUserFlags>();
    public Mock<IUserIndex> UserIndexMock { get; } = TK.CreateMock<IUserIndex>();
    
    // Поддержка FakeTelegramClient для AI Analysis тестов
    private FakeTelegramClient? _fakeTelegramClient;
    
    public IUserBanService CreateRealUserBanService()
    {
        return new UserBanService(
            BotMock.Object,
            MessageServiceMock.Object,
            UserFlowLoggerMock.Object,
            UserBanServiceLoggerMock.Object,
            ViolationTrackerMock.Object,
            AppConfigMock.Object,
            StatisticsServiceMock.Object,
            new GlobalStatsManager(),
            UserManagerMock.Object,
            UserCleanupServiceMock.Object
        );
    }
    
    /// <summary>
    /// Настройка для работы с FakeTelegramClient (для AI Analysis тестов)
    /// </summary>
    public MessageHandlerTestFactory WithFakeTelegramClient(FakeTelegramClient fakeClient)
    {
        _fakeTelegramClient = fakeClient;
        return this;
    }
    public Mock<ILogger<MessageHandler>> LoggerMock { get; } = TK.CreateLoggerMock<MessageHandler>();
    public Mock<ILogger<UserBanService>> UserBanServiceLoggerMock { get; } = TK.CreateLoggerMock<UserBanService>();
    public Mock<ILogger<SuspiciousCommandHandler>> SuspiciousCommandHandlerLoggerMock { get; } = TK.CreateLoggerMock<SuspiciousCommandHandler>();
    public Mock<ISuspiciousUsersStorage> SuspiciousUsersStorageMock { get; } = TK.CreateMock<ISuspiciousUsersStorage>();
    public FakeTelegramClient FakeBotClient { get; } = FakeTelegramClientFactory.Create();

    // Мокаем интерфейсы командных обработчиков
    public Mock<IStartCommandHandler> StartCommandHandlerMock { get; } = TK.CreateMock<IStartCommandHandler>();
    public Mock<ISuspiciousCommandHandler> SuspiciousCommandHandlerMock { get; } = TK.CreateMock<ISuspiciousCommandHandler>();
    public Mock<ICommandRouter> CommandRouterMock { get; } = TK.CreateMock<ICommandRouter>();



    public MessageHandler CreateMessageHandler()
    {
        // Настраиваем NotificationService мок для проксирования вызовов в BotMock
        NotificationServiceMock.Setup(x => x.DeleteAndReportMessage(It.IsAny<Message>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(async (Message message, string reason, bool isSilentMode, CancellationToken ct) =>
            {
                // Имитируем поведение NotificationService: пересылка -> уведомление -> удаление
                try
                {
                    Message adminMessage;
                    
                    // Если используется FakeTelegramClient, отправляем через него
                    if (_fakeTelegramClient != null)
                    {
                        adminMessage = await _fakeTelegramClient.ForwardMessage(AppConfigMock.Object.AdminChatId, message.Chat.Id, message.MessageId, ct);
                        
                        if (!isSilentMode)
                        {
                            try
                            {
                                var replyParams = new ReplyParameters { MessageId = message.MessageId };
                                await _fakeTelegramClient.SendMessage(message.Chat.Id, "⚠️ Ваше сообщение нарушает правила чата", ParseMode.Html, replyParams, null, ct);
                            }
                            catch
                            {
                                // Игнорируем ошибки отправки предупреждения пользователю как в реальном сервисе
                            }
                        }
                        
                        // Отправляем уведомление админу с кнопками
                        var fakeButtons = new InlineKeyboardMarkup(new[] {
                            new[] { InlineKeyboardButton.WithCallbackData("Забанить", $"ban_user_{message.From.Id}") }
                        });
                        await _fakeTelegramClient.SendMessage(AppConfigMock.Object.AdminChatId, $"🚫 {reason}", ParseMode.Html, new ReplyParameters { MessageId = adminMessage.MessageId }, fakeButtons, ct);
                    }
                    else
                    {
                        adminMessage = await BotMock.Object.ForwardMessage(AppConfigMock.Object.AdminChatId, message.Chat.Id, message.MessageId, ct);
                        
                        if (!isSilentMode)
                        {
                            try
                            {
                                var replyParams = new ReplyParameters { MessageId = message.MessageId };
                                await BotMock.Object.SendMessage(message.Chat.Id, "⚠️ Ваше сообщение нарушает правила чата", ParseMode.Html, replyParams, null, ct);
                            }
                            catch
                            {
                                // Игнорируем ошибки отправки предупреждения пользователю как в реальном сервисе
                            }
                        }
                        
                        // Отправляем уведомление админу с кнопками
                        var buttons = new InlineKeyboardMarkup(new[] {
                            new[] { InlineKeyboardButton.WithCallbackData("Забанить", $"ban_user_{message.From.Id}") }
                        });
                        await BotMock.Object.SendMessage(AppConfigMock.Object.AdminChatId, $"🚫 {reason}", ParseMode.Html, new ReplyParameters { MessageId = adminMessage.MessageId }, buttons, ct);
                    }
                }
                catch
                {
                    // Обрабатываем ошибки пересылки/уведомления как в реальном NotificationService
                    try
                    {
                        // Fallback при ошибке пересылки
                        if (_fakeTelegramClient != null)
                        {
                            var fallbackButtons = new InlineKeyboardMarkup(new[] {
                                new[] { InlineKeyboardButton.WithCallbackData("Забанить", $"ban_user_{message.From.Id}") }
                            });
                            await _fakeTelegramClient.SendMessage(AppConfigMock.Object.AdminChatId, $"🚫 {reason}\n\n{message.Text}", ParseMode.Html, null, fallbackButtons, ct);
                        }
                        else
                        {
                            var buttons = new InlineKeyboardMarkup(new[] {
                                new[] { InlineKeyboardButton.WithCallbackData("Забанить", $"ban_user_{message.From.Id}") }
                            });
                            await BotMock.Object.SendMessage(AppConfigMock.Object.AdminChatId, $"🚫 {reason}\n\n{message.Text}", ParseMode.Html, null, buttons, ct);
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки уведомлений как в реальном NotificationService
                    }
                }
                
                // Удаляем оригинальное сообщение
                try
                {
                    if (_fakeTelegramClient != null)
                    {
                        await _fakeTelegramClient.DeleteMessage(message.Chat.Id, message.MessageId, ct);
                    }
                    else
                    {
                        await BotMock.Object.DeleteMessage(message.Chat.Id, message.MessageId, ct);
                    }
                }
                catch
                {
                    // Graceful обработка ошибок удаления (как в реальном NotificationService)
                    // Метод завершается успешно даже при ошибке удаления
                }
            });

        // Настраиваем ButtonsService мок для проксирования вызовов в BotMock  
        ButtonsServiceMock.Setup(x => x.SendSuspiciousMessageWithButtons(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<SuspiciousMessageNotificationData>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(async (Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken ct) =>
            {
                // Обработка null параметров как в реальном ButtonsService
                if (message == null || user == null || data == null)
                {
                    return;
                }
                
                try
                {
                    // Если используется FakeTelegramClient, отправляем через него
                    if (_fakeTelegramClient != null)
                    {
                        var fakeAdminMessage = await _fakeTelegramClient.ForwardMessage(AppConfigMock.Object.AdminChatId, message.Chat.Id, message.MessageId, ct);
                        
                        var fakeButtons = new InlineKeyboardMarkup(new[] {
                            new[] { 
                                InlineKeyboardButton.WithCallbackData("Забанить", $"ban_user_{user.Id}"),
                                InlineKeyboardButton.WithCallbackData("Одобрить", $"approve_user_{user.Id}")
                            }
                        });
                        var fakePrefix = isSilentMode ? "🔇 **Тихий режим**\n\n" : "";
                        await _fakeTelegramClient.SendMessage(AppConfigMock.Object.AdminChatId, $"{fakePrefix}🤔 {data.Reason}", ParseMode.Html, new ReplyParameters { MessageId = fakeAdminMessage.MessageId }, fakeButtons, ct);
                        return;
                    }
                    
                    // Пересылаем сообщение в админ-чат через BotMock
                    var adminMessage = await BotMock.Object.ForwardMessage(AppConfigMock.Object.AdminChatId, message.Chat.Id, message.MessageId, ct);
                    
                    // Отправляем уведомление с кнопками как reply на пересланное сообщение
                    var buttons = new InlineKeyboardMarkup(new[] {
                        new[] { 
                            InlineKeyboardButton.WithCallbackData("Забанить", $"ban_user_{user.Id}"),
                            InlineKeyboardButton.WithCallbackData("Одобрить", $"approve_user_{user.Id}")
                        }
                    });
                    var prefix = isSilentMode ? "🔇 **Тихий режим**\n\n" : "";
                    await BotMock.Object.SendMessage(AppConfigMock.Object.AdminChatId, $"{prefix}🤔 {data.Reason}", ParseMode.Html, new ReplyParameters { MessageId = adminMessage.MessageId }, buttons, ct);
                }
                catch
                {
                    try
                    {
                        // Fallback без пересылки 
                        if (_fakeTelegramClient != null)
                        {
                            var fallbackFakeButtons = new InlineKeyboardMarkup(new[] {
                                new[] { 
                                    InlineKeyboardButton.WithCallbackData("Забанить", $"ban_user_{user.Id}"),
                                    InlineKeyboardButton.WithCallbackData("Одобрить", $"approve_user_{user.Id}")
                                }
                            });
                            var fallbackFakePrefix = isSilentMode ? "🔇 **Тихий режим**\n\n" : "";
                            await _fakeTelegramClient.SendMessage(AppConfigMock.Object.AdminChatId, $"{fallbackFakePrefix}🤔 {data.Reason}\n\n{message.Text}", ParseMode.Html, null, fallbackFakeButtons, ct);
                            return;
                        }
                        
                        // Fallback без пересылки через BotMock
                        var fallbackButtons = new InlineKeyboardMarkup(new[] {
                            new[] { 
                                InlineKeyboardButton.WithCallbackData("Забанить", $"ban_user_{user.Id}"),
                                InlineKeyboardButton.WithCallbackData("Одобрить", $"approve_user_{user.Id}")
                            }
                        });
                        var fallbackPrefix = isSilentMode ? "🔇 **Тихий режим**\n\n" : "";
                        await BotMock.Object.SendMessage(AppConfigMock.Object.AdminChatId, $"{fallbackPrefix}🤔 {data.Reason}\n\n{message.Text}", ParseMode.Html, null, fallbackButtons, ct);
                    }
                    catch
                    {
                        // Последний fallback через MessageService - для тестов sendSuspiciousMessage fallback
                        await MessageServiceMock.Object.SendAdminNotificationAsync(AdminNotificationType.SuspiciousMessage, data, ct);
                    }
                }
            });

        return new MessageHandler(
            BotMock.Object,
            ModerationServiceMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            ClassifierMock.Object,
            BadMessageManagerMock.Object,
            AiChecksMock.Object,
            new GlobalStatsManager(),
            StatisticsServiceMock.Object,
            UserFlowLoggerMock.Object,
            MessageServiceMock.Object,
            ChatLinkFormatterMock.Object,
            BotPermissionsServiceMock.Object,
            AppConfigMock.Object,
            ViolationTrackerMock.Object,
            LoggerMock.Object,
            UserBanServiceMock.Object,
            ChannelModerationServiceMock.Object,
            Mock.Of<IStartCommandHandler>(),
            Mock.Of<ISuspiciousCommandHandler>(),
            CommandRouterMock.Object,
            LogChatServiceMock.Object,
            JoinedUserFlagsMock.Object,
            UserIndexMock.Object,
            AiCascadeServiceMock.Object,
            NotificationServiceMock.Object,
            ForwardingServiceMock.Object,
            ButtonsServiceMock.Object
        );
    }
    
    public MessageHandler CreateMessageHandlerWithRealUserBanService()
    {
        return new MessageHandler(
            BotMock.Object,
            ModerationServiceMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            ClassifierMock.Object,
            BadMessageManagerMock.Object,
            AiChecksMock.Object,
            new GlobalStatsManager(),
            StatisticsServiceMock.Object,
            UserFlowLoggerMock.Object,
            MessageServiceMock.Object,
            ChatLinkFormatterMock.Object,
            BotPermissionsServiceMock.Object,
            AppConfigMock.Object,
            ViolationTrackerMock.Object,
            LoggerMock.Object,
            CreateRealUserBanService(),
            ChannelModerationServiceMock.Object,
            StartCommandHandlerMock.Object,
            SuspiciousCommandHandlerMock.Object,
            CommandRouterMock.Object,
            LogChatServiceMock.Object,
            JoinedUserFlagsMock.Object,
            UserIndexMock.Object,
            AiCascadeServiceMock.Object,
            NotificationServiceMock.Object,
            ForwardingServiceMock.Object,
            ButtonsServiceMock.Object
        );
    }

    #region Configuration Methods

    public MessageHandlerTestFactory WithBotSetup(Action<Mock<ITelegramBotClientWrapper>> setup)
    {
        setup(BotMock);
        return this;
    }

    public MessageHandlerTestFactory WithModerationServiceSetup(Action<Mock<IModerationService>> setup)
    {
        setup(ModerationServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithCaptchaServiceSetup(Action<Mock<ICaptchaService>> setup)
    {
        setup(CaptchaServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithUserManagerSetup(Action<Mock<IUserManager>> setup)
    {
        setup(UserManagerMock);
        return this;
    }

    public MessageHandlerTestFactory WithClassifierSetup(Action<Mock<ISpamHamClassifier>> setup)
    {
        setup(ClassifierMock);
        return this;
    }

    public MessageHandlerTestFactory WithBadMessageManagerSetup(Action<Mock<IBadMessageManager>> setup)
    {
        setup(BadMessageManagerMock);
        return this;
    }

    public MessageHandlerTestFactory WithAiChecksSetup(Action<Mock<IAiChecks>> setup)
    {
        setup(AiChecksMock);
        return this;
    }

    public MessageHandlerTestFactory WithStatisticsServiceSetup(Action<Mock<IStatisticsService>> setup)
    {
        setup(StatisticsServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithServiceProviderSetup(Action<Mock<IServiceProvider>> setup)
    {
        setup(ServiceProviderMock);
        return this;
    }

    public MessageHandlerTestFactory WithUserFlowLoggerSetup(Action<Mock<IUserFlowLogger>> setup)
    {
        setup(UserFlowLoggerMock);
        return this;
    }

    public MessageHandlerTestFactory WithMessageServiceSetup(Action<Mock<IMessageService>> setup)
    {
        setup(MessageServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithChatLinkFormatterSetup(Action<Mock<IChatLinkFormatter>> setup)
    {
        setup(ChatLinkFormatterMock);
        return this;
    }

    public MessageHandlerTestFactory WithLoggerSetup(Action<Mock<ILogger<MessageHandler>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    public MessageHandlerTestFactory WithAppConfigSetup(Action<Mock<IAppConfig>> setup)
    {
        setup(AppConfigMock);
        return this;
    }

    public MessageHandlerTestFactory WithBotPermissionsServiceSetup(Action<Mock<IBotPermissionsService>> setup)
    {
        setup(BotPermissionsServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithViolationTrackerSetup(Action<Mock<IViolationTracker>> setup)
    {
        setup(ViolationTrackerMock);
        return this;
    }

    public MessageHandlerTestFactory WithUserBanServiceSetup(Action<Mock<IUserBanService>> setup)
    {
        setup(UserBanServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithSuspiciousUsersStorageSetup(Action<Mock<ISuspiciousUsersStorage>> setup)
    {
        setup(SuspiciousUsersStorageMock);
        return this;
    }

    public MessageHandlerTestFactory WithSuspiciousCommandHandlerLoggerSetup(Action<Mock<ILogger<SuspiciousCommandHandler>>> setup)
    {
        setup(SuspiciousCommandHandlerLoggerMock);
        return this;
    }

    public MessageHandlerTestFactory WithCommandRouterSetup(Action<Mock<ICommandRouter>> setup)
    {
        setup(CommandRouterMock);
        return this;
    }

    #endregion

    #region Композиционные методы настройки

    /// <summary>
    /// Настройка стандартных моков для всех тестов
    /// </summary>
    public MessageHandlerTestFactory WithStandardMocks()
    {
        WithAppConfigSetup(mock => 
        {
            mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
            mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
            mock.Setup(x => x.AdminChatId).Returns(123456789);
            mock.Setup(x => x.LogAdminChatId).Returns(987654321);
        });
        
        WithBotPermissionsServiceSetup(mock =>
        {
            mock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        });
        
        WithCaptchaServiceSetup(mock =>
        {
            mock.Setup(x => x.GenerateKey(It.IsAny<long>(), It.IsAny<long>()))
                .Returns("test-key");
            mock.Setup(x => x.GetCaptchaInfo(It.IsAny<string>()))
                .Returns((CaptchaInfo?)null);
        });
        
        WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false);
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null);
        });
        
        WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(TK.Specialized.Moderation.Allow());
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        // Настройка ServiceProvider для CommandHandler'ов
        WithServiceProviderSetup(mock =>
        {
            // Создаем реальные экземпляры CommandHandler'ов с правильными логгерами
            var startCommandHandler = new StartCommandHandler(
                BotMock.Object,
                TK.CreateLoggerMock<StartCommandHandler>().Object,
                MessageServiceMock.Object,
                AppConfigMock.Object
            );
            
            var suspiciousCommandHandler = new SuspiciousCommandHandler(
                BotMock.Object,
                ModerationServiceMock.Object,
                MessageServiceMock.Object,
                TK.CreateLoggerMock<SuspiciousCommandHandler>().Object,
                AppConfigMock.Object
            );
            
            // Настраиваем ServiceProvider для возврата CommandHandler'ов
            mock.Setup(x => x.GetService(typeof(StartCommandHandler)))
                .Returns(startCommandHandler);
            mock.Setup(x => x.GetService(typeof(SuspiciousCommandHandler)))
                .Returns(suspiciousCommandHandler);
            
            // Настраиваем ServiceProvider для возврата IChannelModerationService
            mock.Setup(x => x.GetService(typeof(IChannelModerationService)))
                .Returns(ChannelModerationServiceMock.Object);
        });
        
        return this;
    }

    /// <summary>
    /// Настройка моков для сценариев бана
    /// </summary>
    public MessageHandlerTestFactory WithBanMocks()
    {
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
        return this;
    }

    /// <summary>
    /// Настройка моков для сценариев с каналами
    /// </summary>
    public MessageHandlerTestFactory WithChannelMocks()
    {
        WithAppConfigSetup(mock =>
        {
            // ChannelAutoBan отсутствует в эталонной версии momai
            // mock.Setup(x => x.ChannelAutoBan).Returns(true);
        });
        
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
        
        return this;
    }

    /// <summary>
    /// Настройка моков для сценариев модерации
    /// </summary>
    public MessageHandlerTestFactory WithModerationMocks(ModerationAction action = ModerationAction.Allow, string reason = "Test moderation")
    {
        WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(action, reason));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        return this;
    }

    #endregion

    #region Factory Methods

    public FakeTelegramClient FakeTelegramClient => FakeTelegramClientFactory.Create();

    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock { get; } = new();

    public ModerationService CreateModerationServiceWithFake()
    {
        var mockLogger = new Mock<ILogger<ModerationService>>();
        var mockClassifier = new Mock<ISpamHamClassifier>();
        var mockMimicryClassifier = new Mock<IMimicryClassifier>();
        var mockBadMessageManager = new Mock<IBadMessageManager>();
        var mockUserManager = new Mock<IUserManager>();
        var mockAiChecks = new Mock<IAiChecks>();
        var mockSuspiciousUsersStorage = new Mock<ISuspiciousUsersStorage>();
        var mockMessageService = new Mock<IMessageService>();

        return new ModerationService(
            mockClassifier.Object,
            mockMimicryClassifier.Object,
            mockBadMessageManager.Object,
            mockUserManager.Object,
            mockAiChecks.Object,
            mockSuspiciousUsersStorage.Object,
            FakeBotClient as ITelegramBotClient,
            mockMessageService.Object,
            UserBanServiceMock.Object,
            new Mock<IUserCleanupService>().Object,
            mockLogger.Object
        );
    }

            public CaptchaService CreateCaptchaServiceWithFake()
        {
            var mockLogger = new Mock<ILogger<CaptchaService>>();
            var mockMessageService = new Mock<IMessageService>();
            var mockAppConfig = new Mock<IAppConfig>();
            return new CaptchaService(TelegramBotClientWrapperMock.Object, mockLogger.Object, mockMessageService.Object, mockAppConfig.Object, ViolationTrackerMock.Object, new Mock<IUserBanService>().Object);
        }

    public IUserManager CreateUserManagerWithFake()
    {
        var mockLogger = new Mock<ILogger<UserManager>>();
        var mockApprovedUsersLogger = new Mock<ILogger<ApprovedUsersStorage>>();
        var approvedUsersStorage = new ApprovedUsersStorage(mockApprovedUsersLogger.Object);
        var mockAppConfig = new Mock<IAppConfig>();
        return new UserManager(mockLogger.Object, approvedUsersStorage, mockAppConfig.Object);
    }

    public async Task<MessageHandler> CreateAsync()
    {
        return CreateMessageHandler();
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        var mockLogger = new Mock<ILogger<SpamHamClassifier>>();
        return new SpamHamClassifier(mockLogger.Object);
    }

    public MessageHandler CreateMessageHandlerWithFake()
    {
        return CreateMessageHandlerWithFake(FakeTelegramClientFactory.Create());
    }

    public MessageHandler CreateMessageHandlerWithFake(FakeTelegramClient fakeClient)
    {
        // Настраиваем ServiceProvider для возврата IChannelModerationService если еще не настроен
        if (!ServiceProviderMock.Setups.Any(s => s.ToString().Contains("IChannelModerationService")))
        {
            ServiceProviderMock.Setup(x => x.GetService(typeof(IChannelModerationService)))
                .Returns(ChannelModerationServiceMock.Object);
        }
        
        // Настраиваем TelegramBotClientWrapperMock для работы с FakeTelegramClient
        TelegramBotClientWrapperMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<ChatId, int, CancellationToken>((chatId, messageId, token) =>
            {
                fakeClient.DeleteMessage(chatId, messageId, token);
            });

        return new MessageHandler(
            TelegramBotClientWrapperMock.Object,
            ModerationServiceMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            ClassifierMock.Object,
            BadMessageManagerMock.Object,
            AiChecksMock.Object,
            new GlobalStatsManager(),
            StatisticsServiceMock.Object,
            UserFlowLoggerMock.Object,
            MessageServiceMock.Object,
            ChatLinkFormatterMock.Object,
            BotPermissionsServiceMock.Object,
            AppConfigMock.Object,
            ViolationTrackerMock.Object,
            LoggerMock.Object,
            UserBanServiceMock.Object,
            ChannelModerationServiceMock.Object,
            StartCommandHandlerMock.Object,
            SuspiciousCommandHandlerMock.Object,
            CommandRouterMock.Object,
            LogChatServiceMock.Object,
            JoinedUserFlagsMock.Object,
            UserIndexMock.Object,
            AiCascadeServiceMock.Object,
            NotificationServiceMock.Object,
            ForwardingServiceMock.Object,
            ButtonsServiceMock.Object
        );
    }

    public MessageHandler CreateMessageHandlerWithFake(Action<MessageHandlerTestFactory> setup)
    {
        setup(this);
        return CreateMessageHandler();
    }

    #endregion

    #region Ban Test Scenarios

    /// <summary>
    /// Настраивает моки для сценария бана пользователя с длинным именем
    /// </summary>
    public MessageHandlerTestFactory SetupLongNameBanScenario(User user)
    {
        UserManagerMock.Setup(x => x.Approved(user.Id, null)).Returns(false);
        UserManagerMock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(false);
        UserManagerMock.Setup(x => x.GetClubUsername(user.Id)).ReturnsAsync((string?)null);
        ModerationServiceMock.Setup(x => x.CheckUserNameAsync(user))
            .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Длинное имя пользователя"));
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария бана пользователя из блэклиста
    /// </summary>
    public MessageHandlerTestFactory SetupBlacklistBanScenario(User user, Chat chat)
    {
        UserManagerMock.Setup(x => x.Approved(user.Id, null)).Returns(false);
        UserManagerMock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(true);
        UserManagerMock.Setup(x => x.GetClubUsername(user.Id)).ReturnsAsync((string?)null);
        ModerationServiceMock.Setup(x => x.CheckUserNameAsync(user))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Нормальное имя"));
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария обработки бана из блэклиста
    /// </summary>
    public MessageHandlerTestFactory SetupBlacklistBanHandlingScenario(User user, string reason)
    {
        UserManagerMock.Setup(x => x.Approved(user.Id, null)).Returns(false);
        UserManagerMock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(true);
        ModerationServiceMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Ban, reason));
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария удаления по результату модерации (ModerationAction.Delete)
    /// </summary>
    public MessageHandlerTestFactory SetupModerationDeleteScenario(string reason = "ML решил что это спам")
    {
        SetupStandardBanTestScenario();
        
        WithModerationServiceSetup(mock =>
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Delete, reason));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        // Настраиваем BotMock для обработки ForwardMessage
        WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 123456789 } });
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 123456789 } });
        });
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария AI подтверждения ML подозрения
    /// </summary>
    public MessageHandlerTestFactory SetupAiMlBanScenario(double probability = 0.8, string reason = "ML подозрение")
    {
        SetupStandardBanTestScenario();
        
        WithModerationServiceSetup(mock =>
        {
            // RequireAiReview отсутствует в эталонной версии momai
            // mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            //     .ReturnsAsync(new ModerationResult(ModerationAction.RequireAiReview, reason, 0.75));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        WithAiChecksSetup(mock =>
        {
            // GetMlSuspiciousMessageAnalysis отсутствует в эталонной версии momai
            // mock.Setup(x => x.GetMlSuspiciousMessageAnalysis(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<double>()))
            //     .ReturnsAsync(new SpamProbability { Probability = probability, Reason = reason });
        });
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария AI отклонения ML подозрения
    /// </summary>
    public MessageHandlerTestFactory SetupAiMlRejectScenario(double probability = 0.3, string reason = "ML подозрение")
    {
        SetupStandardBanTestScenario();
        
        WithModerationServiceSetup(mock =>
        {
            // RequireAiReview отсутствует в эталонной версии momai
            // mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            //     .ReturnsAsync(new ModerationResult(ModerationAction.RequireAiReview, reason, 0.75));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        WithAiChecksSetup(mock =>
        {
            // GetMlSuspiciousMessageAnalysis отсутствует в эталонной версии momai
            // mock.Setup(x => x.GetMlSuspiciousMessageAnalysis(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<double>()))
            //     .ReturnsAsync(new SpamProbability { Probability = probability, Reason = reason });
        });
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария повторных нарушений
    /// </summary>
    public MessageHandlerTestFactory SetupRepeatedViolationsBanScenario(string violationType = "TextMention")
    {
        SetupStandardBanTestScenario();
        
        WithViolationTrackerSetup(mock =>
        {
            mock.Setup(x => x.RegisterViolation(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<ViolationType>()))
                .Returns(true); // Возвращаем true, что означает необходимость бана
        });
        
        return this;
    }

    /// <summary>
    /// Настройка стандартных моков для тестов бана (повторяющаяся логика)
    /// </summary>
    public MessageHandlerTestFactory SetupStandardBanTestScenario()
    {
        return this
            .WithStandardMocks()
            .WithBanMocks();
    }

    /// <summary>
    /// Настройка моков для сценария автобана пользователя
    /// </summary>
    public MessageHandlerTestFactory SetupAutoBanScenario(User user, string reason = "Автобан")
    {
        return this
            .WithStandardMocks()
            .WithBanMocks()
            .WithModerationMocks(ModerationAction.Ban, reason)
            .WithUserManagerSetup(mock =>
            {
                mock.Setup(x => x.Approved(user.Id, null)).Returns(false);
                mock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(false);
            });
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
    }

    /// <summary>
    /// Настройка моков для сценария автобана каналов
    /// </summary>
    public MessageHandlerTestFactory SetupChannelAutoBanScenario()
    {
        return this
            .WithStandardMocks()
            .WithBanMocks()
            .WithChannelMocks();
    }

    /// <summary>
    /// Настройка моков для сценария бана по результату модерации (ModerationAction.Ban)
    /// </summary>
    public MessageHandlerTestFactory SetupModerationBanScenario(string reason = "Спам сообщение")
    {
        return this
            .WithStandardMocks()
            .WithBanMocks()
            .WithModerationMocks(ModerationAction.Ban, reason);
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
    }

    /// <summary>
    /// Настраивает стандартные моки для тестов с длинным именем пользователя
    /// </summary>
    public MessageHandlerTestFactory SetupLongNameBanTestScenario(User user)
    {
        SetupStandardBanTestScenario();
        
        WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(user.Id, null)).Returns(false);
            mock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(false);
            mock.Setup(x => x.GetClubUsername(user.Id)).ReturnsAsync((string?)null);
        });
        
        WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
            mock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Длинное имя пользователя"));
        });
        
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
        
        return this;
    }

    /// <summary>
    /// Настраивает стандартные моки для тестов с пользователем в блэклисте
    /// </summary>
    public MessageHandlerTestFactory SetupBlacklistUserTestScenario(User user)
    {
        SetupStandardBanTestScenario();
        
        WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(user.Id, null)).Returns(false);
            mock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(true); // Пользователь в блэклисте
            mock.Setup(x => x.GetClubUsername(user.Id)).ReturnsAsync((string?)null);
        });
        
        WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
            mock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid username"));
        });
        
        return this;
    }

    /// <summary>
    /// Настройка моков для сценария с сообщением от канала в заданном чате
    /// </summary>
    public MessageHandlerTestFactory SetupChannelTestScenario(Chat chat)
    {
        // Базовые настройки окружения
        WithStandardMocks();
        WithChannelMocks();

        // По умолчанию обработка сообщений каналов не должна кидать исключения
        ChannelModerationServiceMock
            .Setup(x => x.HandleChannelMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return this;
    }

    #endregion

    #region AI Analysis Test Methods

    /// <summary>
    /// Создает MessageHandler для AI Analysis тестов с FakeTelegramClient
    /// Используется для совместимости с существующими E2E тестами
    /// </summary>
    public MessageHandler CreateMessageHandlerForAiAnalysisTests(FakeTelegramClient fakeBot, IAppConfig appConfig)
    {
        // Настраиваем фабрику для работы с FakeTelegramClient
        WithFakeTelegramClient(fakeBot);
        
        // Настраиваем AppConfig для тестов
        AppConfigMock.Setup(x => x.AdminChatId).Returns(appConfig.AdminChatId);
        AppConfigMock.Setup(x => x.LogAdminChatId).Returns(appConfig.LogAdminChatId);
        AppConfigMock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
        AppConfigMock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
        
        // Настраиваем стандартные моки для AI анализа
        WithStandardMocks();
        
        // Специальная настройка для AI analysis тестов - пользователи НЕ одобрены
        WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>()))
                .Returns(false); // Важно: НЕ одобрены для запуска AI анализа
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false);
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null);
        });
        
        // Настраиваем AiCascadeService для возврата подозрительного профиля
        AiCascadeServiceMock.Setup(x => x.PerformAiProfileAnalysisAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<CancellationToken>()))
            .Returns(async (Message message, User user, Chat chat, CancellationToken ct) =>
            {
                // Имитируем отправку подозрительного сообщения через ButtonsService
                var data = new SuspiciousMessageNotificationData(
                    user, 
                    chat, 
                    message.Text ?? message.Caption ?? "",
                    message.MessageId
                );
                data.Reason = "AI анализ профиля";
                
                // Используем FakeTelegramClient для отправки уведомления, если он настроен
                if (_fakeTelegramClient != null)
                {
                    var buttons = new InlineKeyboardMarkup(new[] {
                        new[] { 
                            InlineKeyboardButton.WithCallbackData("Забанить", $"ban_user_{user.Id}"),
                            InlineKeyboardButton.WithCallbackData("Одобрить", $"approve_user_{user.Id}")
                        }
                    });
                    await _fakeTelegramClient.SendMessage(appConfig.AdminChatId, $"🤔 {data.Reason}", ParseMode.Html, null, buttons, ct);
                }
                
                return true; // Профиль подозрительный
            });
        
        return CreateMessageHandler();
    }

    #endregion
}
