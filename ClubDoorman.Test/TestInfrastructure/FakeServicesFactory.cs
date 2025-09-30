using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Models.Requests;
using ClubDoorman.Services.Logging;
using ClubDoorman.Models.Logging;
using ClubDoorman.Services;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static ClubDoorman.Services.AI.AiChecks;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Notifications;
using ClubDoorman.Services.ClickHouse;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Фабрика для создания фейковых сервисов
/// Упрощает создание и настройку фейковых сервисов для тестов
/// </summary>
public class FakeServicesFactory
{
    private readonly FakeTelegramClient _fakeBot;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IAppConfig _appConfig;

    public FakeServicesFactory(
        FakeTelegramClient? fakeBot = null,
        ILoggerFactory? loggerFactory = null,
        IAppConfig? appConfig = null)
    {
        _fakeBot = fakeBot ?? new FakeTelegramClient();
        _loggerFactory = loggerFactory ?? LoggerFactory.Create(builder => builder.AddConsole());
        _appConfig = appConfig ?? new AppConfig(
            Options.Create(new AutoBanOptions()),
            Options.Create(new ViolationThresholdOptions()),
            Options.Create(new FeatureToggleOptions()),
            Options.Create(new ChatFilteringOptions()),
            Options.Create(new CoreOptions()),
            Options.Create(new ChatAccessOptions()),
            Options.Create(new AiOptions()),
            Options.Create(new RabbitMqOptions()),
            Options.Create(new ClickHouseOptions()),
            Options.Create(new TestHarnessOptions())
        );
    }

    /// <summary>
    /// Создает фейковый сервис капчи
    /// </summary>
    public FakeCaptchaService CreateCaptchaService()
    {
        var messageService = new Mock<IMessageService>().Object;
        var logger = _loggerFactory.CreateLogger<FakeCaptchaService>();

        return new FakeCaptchaService(_fakeBot, logger, messageService, _appConfig, new Mock<IViolationTracker>().Object);
    }

    /// <summary>
    /// Создает фейковый обработчик callback query
    /// </summary>
    public FakeCallbackQueryHandler CreateCallbackQueryHandler()
    {
        var userManager = new Mock<IUserManager>().Object;
        var logger = _loggerFactory.CreateLogger<FakeCallbackQueryHandler>();

        return new FakeCallbackQueryHandler(_fakeBot, logger, userManager, _appConfig);
    }

    /// <summary>
    /// Создает фейковый сервис модерации
    /// </summary>
    public FakeModerationService CreateModerationService()
    {
        var classifier = new Mock<ISpamHamClassifier>().Object;
        var mimicryClassifier = new Mock<IMimicryClassifier>().Object;
        var badMessageManager = new Mock<IBadMessageManager>().Object;
        var userManager = new Mock<IUserManager>().Object;
        var aiChecks = new Mock<IAiChecks>().Object;
        var suspiciousUsersStorage = new Mock<ISuspiciousUsersStorage>().Object;
        var messageService = new Mock<IMessageService>().Object;
        var logger = _loggerFactory.CreateLogger<FakeModerationService>();

        return new FakeModerationService(
            classifier, mimicryClassifier, badMessageManager, userManager,
            aiChecks, suspiciousUsersStorage, _fakeBot, messageService,
            new Mock<IUserBanService>().Object, logger);
    }

    /// <summary>
    /// Создает фейковый MessageHandler с настраиваемыми сервисами
    /// </summary>
    public MessageHandler CreateMessageHandler(
        FakeCaptchaService? captchaService = null,
        FakeModerationService? moderationService = null)
    {
        // Создаем моки с правильной настройкой
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(x => x.Approved(It.IsAny<long>(), null)).Returns(false);
        userManagerMock.Setup(x => x.InBanlist(It.IsAny<long>()))
            .ReturnsAsync(false)
            .Callback<long>(userId => Console.WriteLine($"🔍 TRACE: [Mock] InBanlist вызван для userId={userId}"));
        userManagerMock.Setup(x => x.Approve(It.IsAny<long>(), null)).Returns(ValueTask.CompletedTask);

        // Настраиваем мок для IModerationService
        var moderationServiceMock = new Mock<IModerationService>();
        moderationServiceMock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>())).Returns(false);
        moderationServiceMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>())).ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Test message"));

        // Настраиваем мок фасада модерации так, чтобы возвращался Allow (нужно для запуска AI анализа профиля)
        var moderationFacadeMock = new Mock<IModerationFacade>();
        moderationFacadeMock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>())).Returns(false);
        moderationFacadeMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Allow for AI analysis"));

        var classifierMock = new Mock<ISpamHamClassifier>();
        classifierMock.Setup(x => x.IsSpam(It.IsAny<string>())).ReturnsAsync((false, 0.5f));

        var badMessageManagerMock = new Mock<IBadMessageManager>();
        badMessageManagerMock.Setup(x => x.KnownBadMessage(It.IsAny<string>())).Returns(false);

        var aiChecksMock = new Mock<IAiChecks>();
        aiChecksMock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), It.IsAny<Func<string, Task>>()))
            .ReturnsAsync(new SpamPhotoBio(new SpamProbability { Probability = 0.9, Reason = "Test suspicious profile" }, new byte[0], "Test bio"));
        aiChecksMock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<Func<string, Task>>()))
            .ReturnsAsync(new SpamPhotoBio(new SpamProbability { Probability = 0.9, Reason = "Test suspicious profile with message" }, new byte[0], "Test bio"));

        var globalStatsManager = new GlobalStatsManager();

        var appConfigMock = new Mock<IAppConfig>();
        var violationTrackerMock = new Mock<IViolationTracker>();
        var statisticsServiceMock = new Mock<IStatisticsService>();
        var globalStatsManagerMock = new Mock<GlobalStatsManager>();



        statisticsServiceMock.Setup(x => x.GetAllStats()).Returns(new Dictionary<long, ChatStats>());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(It.IsAny<Type>())).Returns(null);

        var userFlowLoggerMock = new Mock<IUserFlowLogger>();
        userFlowLoggerMock.Setup(x => x.LogFirstMessage(It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<string>()));

        var messageServiceMock = new Mock<IMessageService>();
        messageServiceMock.Setup(x => x.SendWelcomeMessageAsync(It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Telegram.Bot.Types.Message());
        messageServiceMock.Setup(x => x.SendWelcomeMessageAsync(It.IsAny<SendWelcomeMessageRequest>()))
            .ReturnsAsync(new Telegram.Bot.Types.Message());
        messageServiceMock.Setup(x => x.SendAiProfileAnalysisAsync(It.IsAny<AiProfileAnalysisData>(), It.IsAny<CancellationToken>()))
            .Callback<AiProfileAnalysisData, CancellationToken>((data, token) =>
            {
                // Отправляем реальное сообщение через FakeTelegramClient
                _fakeBot.SendMessageAsync(_appConfig.AdminChatId,
                    $"AI анализ профиля пользователя {data.User.FirstName} {data.User.LastName} (@{data.User.Username})");
            })
            .Returns(Task.CompletedTask);

        // Настраиваем каскадный сервис AI так, чтобы он вызывал отправку уведомления через messageService
        var aiCascadeServiceMock = new Mock<IAiCascadeService>();
        aiCascadeServiceMock
            .Setup(x => x.PerformAiProfileAnalysisAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<CancellationToken>()))
            .Callback<Message, User, Chat, CancellationToken>((msg, user, chat, ct) =>
            {
                // эмулируем успешный анализ профиля
                var data = new AiProfileAnalysisData(
                    user,
                    chat,
                    0.93,
                    "Test suspicious profile",
                    "Test bio",
                    msg.Text ?? string.Empty,
                    Array.Empty<byte>(),
                    msg.MessageId,
                    "🗑️ Test automatic action");
                messageServiceMock.Object.SendAiProfileAnalysisAsync(data, ct);
            })
            .ReturnsAsync(true);

        var chatLinkFormatterMock = new Mock<IChatLinkFormatter>();
        chatLinkFormatterMock.Setup(x => x.GetChatLink(It.IsAny<long>(), It.IsAny<string>())).Returns("https://t.me/test");

        var botPermissionsServiceMock = new Mock<IBotPermissionsService>();
        botPermissionsServiceMock.Setup(x => x.IsBotAdminAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        botPermissionsServiceMock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        botPermissionsServiceMock.Setup(x => x.GetBotChatMemberAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((ChatMember?)null);

        // Настраиваем ServiceProvider для возврата IChannelModerationService
        serviceProviderMock.Setup(x => x.GetService(typeof(IChannelModerationService)))
            .Returns(new Mock<IChannelModerationService>().Object);

        var logger = _loggerFactory.CreateLogger<MessageHandler>();

        // Build full pipeline (10-220) mirroring production so AI analysis + moderation semantics happen inside pipeline.
        var gmRecorder = new GoldenMasterRecorder(Options.Create(new LoggingFlagsOptions { GoldenMasterEnabled = false }), new Mock<ILogger<GoldenMasterRecorder>>().Object);
        var eventsPublisher = new Mock<IModerationEventPublisher>().Object; // semantics ignored in these integration tests
        var commandRouterMock = new Mock<ICommandRouter>();
        var userJoinFacadeMock = new Mock<IUserJoinFacade>();
        var channelModerationMock = new Mock<IChannelModerationService>();
        var forwardingMock = new Mock<IForwardingService>();

        var steps = new List<ClubDoorman.Services.Handlers.Pipeline.IMessageStep>
        {
            new ClubDoorman.Services.Handlers.Pipeline.Steps.CommandStep(commandRouterMock.Object, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.CommandStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.SystemOrBotMessageStep(eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.SystemOrBotMessageStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.NewMembersStep(userJoinFacadeMock.Object, _appConfig, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.NewMembersStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.LeftMemberCleanupStep(_fakeBot, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.LeftMemberCleanupStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.ChannelMessageStep(channelModerationMock.Object, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.ChannelMessageStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.PrivateSkipStep(eventsPublisher, _appConfig, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.PrivateSkipStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.CaptchaPendingStep(captchaService ?? CreateCaptchaService(), _fakeBot, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.CaptchaPendingStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.BanlistCheckStep(userManagerMock.Object, new Mock<IUserBanService>().Object, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.BanlistCheckStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.AlreadyApprovedStep(moderationFacadeMock.Object, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.AlreadyApprovedStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.FirstMessageLogStep(userFlowLoggerMock.Object, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.FirstMessageLogStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.ClickHouseIngestStep(ClubDoorman.Services.ClickHouse.NullClickHouseMessageSink.Instance, Options.Create(new ClubDoorman.Services.ClickHouse.ClickHouseOptions()), _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.ClickHouseIngestStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.ClubMemberSkipStep(userManagerMock.Object, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.ClubMemberSkipStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.BaseModerationStep(moderationFacadeMock.Object, userFlowLoggerMock.Object, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.BaseModerationStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.AiProfileAnalysisStep(aiCascadeServiceMock.Object, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.AiProfileAnalysisStep>()),
            new ClubDoorman.Services.Handlers.Pipeline.Steps.FinalModerationActionStep(moderationFacadeMock.Object, eventsPublisher, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.Steps.FinalModerationActionStep>())
        };
        var pipeline = new ClubDoorman.Services.Handlers.Pipeline.MessagePipeline(steps, _loggerFactory.CreateLogger<ClubDoorman.Services.Handlers.Pipeline.MessagePipeline>());

        return new MessageHandler(
            _fakeBot,
            _appConfig,
            channelModerationMock.Object,
            commandRouterMock.Object,
            logger,
            botPermissionsServiceMock.Object,
            gmRecorder,
            eventsPublisher,
            Options.Create(new LoggingFlagsOptions { GoldenMasterEnabled = false }),
            pipeline);
    }

    /// <summary>
    /// Создает предустановленные сценарии для тестирования
    /// </summary>
    public static class Scenarios
    {
        /// <summary>
        /// Сценарий успешного прохождения капчи
        /// </summary>
        public static FakeCaptchaService SuccessfulCaptcha(FakeServicesFactory factory)
        {
            return factory.CreateCaptchaService()
                .SetResult(true);
        }

        /// <summary>
        /// Сценарий неудачного прохождения капчи
        /// </summary>
        public static FakeCaptchaService FailedCaptcha(FakeServicesFactory factory)
        {
            return factory.CreateCaptchaService()
                .SetResult(false);
        }

        /// <summary>
        /// Сценарий таймаута капчи
        /// </summary>
        public static FakeCaptchaService TimeoutCaptcha(FakeServicesFactory factory)
        {
            return factory.CreateCaptchaService()
                .SetResponseTime(TimeSpan.FromSeconds(30))
                .SetResult(false);
        }

        /// <summary>
        /// Сценарий успешного одобрения пользователя
        /// </summary>
        public static FakeCallbackQueryHandler SuccessfulApproval(FakeServicesFactory factory)
        {
            return factory.CreateCallbackQueryHandler()
                .SetShouldAnswerCallback(true);
        }

    /// <summary>
    /// Сценарий бана пользователя
    /// </summary>
        public static FakeCallbackQueryHandler UserBan(FakeServicesFactory factory)
        {
            return factory.CreateCallbackQueryHandler()
                .SetShouldAnswerCallback(true);
        }

        /// <summary>
        /// Сценарий безопасного сообщения
        /// </summary>
        public static FakeModerationService SafeMessage(FakeServicesFactory factory)
        {
            return factory.CreateModerationService()
                .SetResult(new ModerationResult(ModerationAction.Allow, "Безопасно"));
        }

        /// <summary>
        /// Сценарий спам-сообщения
        /// </summary>
        public static FakeModerationService SpamMessage(FakeServicesFactory factory)
        {
            return factory.CreateModerationService()
                .SetResult(new ModerationResult(ModerationAction.Delete, "Обнаружен спам", 0.9));
        }

        /// <summary>
        /// Сценарий мимикрии
        /// </summary>
        public static FakeModerationService MimicryMessage(FakeServicesFactory factory)
        {
            return factory.CreateModerationService()
                .SetResult(new ModerationResult(ModerationAction.Report, "Обнаружена мимикрия", 0.8));
        }
    }
}