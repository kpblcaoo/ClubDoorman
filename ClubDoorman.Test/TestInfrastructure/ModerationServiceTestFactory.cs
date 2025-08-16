using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для ModerationService
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ModerationServiceTestFactory
{
    public Mock<ISpamHamClassifier> ClassifierMock { get; } = new();
    public Mock<IMimicryClassifier> MimicryClassifierMock { get; } = new();
    public Mock<IBadMessageManager> BadMessageManagerMock { get; } = new();
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<IAiChecks> AiChecksMock { get; } = new();
    public Mock<ISuspiciousUsersStorage> SuspiciousUsersStorageMock { get; } = new();
    public Mock<ITelegramBotClient> BotClientMock { get; } = new();
    public Mock<IMessageService> MessageServiceMock { get; } = new();
    public Mock<IUserBanService> UserBanServiceMock { get; } = new();
    public Mock<ILogger<ModerationServiceAdapter>> LoggerMock { get; } = new();

    public ModerationServiceAdapter CreateModerationService()
    {
        return new ModerationServiceAdapter(
            new Mock<IModerationPolicy>().Object
        );
    }

    #region Configuration Methods

    public ModerationServiceTestFactory WithClassifierSetup(Action<Mock<ISpamHamClassifier>> setup)
    {
        setup(ClassifierMock);
        return this;
    }

    public ModerationServiceTestFactory WithMimicryClassifierSetup(Action<Mock<IMimicryClassifier>> setup)
    {
        setup(MimicryClassifierMock);
        return this;
    }

    public ModerationServiceTestFactory WithBadMessageManagerSetup(Action<Mock<IBadMessageManager>> setup)
    {
        setup(BadMessageManagerMock);
        return this;
    }

    public ModerationServiceTestFactory WithUserManagerSetup(Action<Mock<IUserManager>> setup)
    {
        setup(UserManagerMock);
        return this;
    }

    public ModerationServiceTestFactory WithAiChecksSetup(Action<Mock<IAiChecks>> setup)
    {
        setup(AiChecksMock);
        return this;
    }

    public ModerationServiceTestFactory WithSuspiciousUsersStorageSetup(Action<Mock<ISuspiciousUsersStorage>> setup)
    {
        setup(SuspiciousUsersStorageMock);
        return this;
    }

    public ModerationServiceTestFactory WithBotClientSetup(Action<Mock<ITelegramBotClient>> setup)
    {
        setup(BotClientMock);
        return this;
    }

    public ModerationServiceTestFactory WithMessageServiceSetup(Action<Mock<IMessageService>> setup)
    {
        setup(MessageServiceMock);
        return this;
    }

    public ModerationServiceTestFactory WithUserBanServiceSetup(Action<Mock<IUserBanService>> setup)
    {
        setup(UserBanServiceMock);
        return this;
    }

    public ModerationServiceTestFactory WithLoggerSetup(Action<Mock<ILogger<ModerationServiceAdapter>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion

    #region Smart Methods Based on Business Logic

    public FakeTelegramClient FakeTelegramClient => FakeTelegramClientFactory.Create();
    
    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock => new Mock<ITelegramBotClientWrapper>();

    public IUserManager CreateUserManagerWithFake()
    {
        return new Mock<IUserManager>().Object;
    }

    public async Task<ModerationServiceAdapter> CreateAsync()
    {
        return await Task.FromResult(CreateModerationService());
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        return new SpamHamClassifier(
            new Mock<ILogger<SpamHamClassifier>>().Object
        );
    }
    #endregion
}
