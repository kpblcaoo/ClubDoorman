using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для MessageHandler
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class MessageHandlerTestFactory
{
    public Mock<ITelegramBotClientWrapper> BotMock { get; } = new();
    public Mock<IModerationService> ModerationServiceMock { get; } = new();
    public Mock<ICaptchaService> CaptchaServiceMock { get; } = new();
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<ISpamHamClassifier> ClassifierMock { get; } = new();
    public Mock<BadMessageManager> BadMessageManagerMock { get; } = new();
    public Mock<IAiChecks> AiChecksMock { get; } = new();
    public Mock<IStatisticsService> StatisticsServiceMock { get; } = new();
    public Mock<IServiceProvider> ServiceProviderMock { get; } = new();
    public Mock<UserFlowLogger> UserFlowLoggerMock { get; } = new();
    public Mock<IMessageService> MessageServiceMock { get; } = new();
    public Mock<ChatLinkFormatter> ChatLinkFormatterMock { get; } = new();
    public Mock<BotPermissionsService> BotPermissionsServiceMock { get; } = new();
    public Mock<ILogger<MessageHandler>> LoggerMock { get; } = new();

    public MessageHandler CreateMessageHandler()
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
            ServiceProviderMock.Object,
            UserFlowLoggerMock.Object,
            MessageServiceMock.Object,
            ChatLinkFormatterMock.Object,
            BotPermissionsServiceMock.Object,
            LoggerMock.Object
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

    public MessageHandlerTestFactory WithBadMessageManagerSetup(Action<Mock<BadMessageManager>> setup)
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

    public MessageHandlerTestFactory WithUserFlowLoggerSetup(Action<Mock<UserFlowLogger>> setup)
    {
        setup(UserFlowLoggerMock);
        return this;
    }

    public MessageHandlerTestFactory WithMessageServiceSetup(Action<Mock<IMessageService>> setup)
    {
        setup(MessageServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithChatLinkFormatterSetup(Action<Mock<ChatLinkFormatter>> setup)
    {
        setup(ChatLinkFormatterMock);
        return this;
    }

    public MessageHandlerTestFactory WithLoggerSetup(Action<Mock<ILogger<MessageHandler>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion

    #region Smart Methods Based on Business Logic

    public FakeTelegramClient FakeTelegramClient => new FakeTelegramClient();
    
    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock => new Mock<ITelegramBotClientWrapper>();

    public ModerationService CreateModerationServiceWithFake()
    {
        return new ModerationService(
            new Mock<ISpamHamClassifier>().Object,
            new Mock<IMimicryClassifier>().Object,
            new Mock<BadMessageManager>().Object,
            new Mock<IUserManager>().Object,
            new Mock<IAiChecks>().Object,
            new Mock<ISuspiciousUsersStorage>().Object,
            new Mock<ITelegramBotClient>().Object,
            new Mock<IMessageService>().Object,
            new Mock<ILogger<ModerationService>>().Object
        );
    }

    public CaptchaService CreateCaptchaServiceWithFake()
    {
        return new CaptchaService(
            new Mock<ITelegramBotClientWrapper>().Object,
            new Mock<ILogger<CaptchaService>>().Object,
            new Mock<IMessageService>().Object
        );
    }

    public IUserManager CreateUserManagerWithFake()
    {
        return new Mock<IUserManager>().Object;
    }

    public async Task<MessageHandler> CreateAsync()
    {
        return await Task.FromResult(CreateMessageHandler());
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        return new SpamHamClassifier(
            new Mock<ILogger<SpamHamClassifier>>().Object
        );
    }

    public MessageHandler CreateMessageHandlerWithFake()
    {
        return CreateMessageHandler();
    }
    
    public MessageHandler CreateMessageHandlerWithFake(FakeTelegramClient fakeClient)
    {
        return new MessageHandler(
            fakeClient,
            ModerationServiceMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            ClassifierMock.Object,
            BadMessageManagerMock.Object,
            AiChecksMock.Object,
            new GlobalStatsManager(),
            StatisticsServiceMock.Object,
            ServiceProviderMock.Object,
            UserFlowLoggerMock.Object,
            MessageServiceMock.Object,
            ChatLinkFormatterMock.Object,
            BotPermissionsServiceMock.Object,
            LoggerMock.Object
        );
    }
    
    public MessageHandler CreateMessageHandlerWithFake(Action<MessageHandlerTestFactory> setup)
    {
        setup(this);
        return CreateMessageHandler();
    }
    #endregion
}
