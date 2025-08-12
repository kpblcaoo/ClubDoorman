using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Violation;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для StatisticsService
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class StatisticsServiceTestFactory
{
    public Mock<ITelegramBotClientWrapper> BotMock { get; } = new();
    public Mock<ILogger<StatisticsService>> LoggerMock { get; } = new();
    public Mock<IChatLinkFormatter> ChatLinkFormatterMock { get; } = new();

    public StatisticsService CreateStatisticsService()
    {
        return new StatisticsService(
            BotMock.Object,
            LoggerMock.Object,
            ChatLinkFormatterMock.Object
        );
    }

    #region Configuration Methods

    public StatisticsServiceTestFactory WithBotSetup(Action<Mock<ITelegramBotClientWrapper>> setup)
    {
        setup(BotMock);
        return this;
    }

    public StatisticsServiceTestFactory WithLoggerSetup(Action<Mock<ILogger<StatisticsService>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    public StatisticsServiceTestFactory WithChatLinkFormatterSetup(Action<Mock<IChatLinkFormatter>> setup)
    {
        setup(ChatLinkFormatterMock);
        return this;
    }

    #endregion

    #region Smart Methods Based on Business Logic

    public FakeTelegramClient FakeTelegramClient => FakeTelegramClientFactory.Create();
    
    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock => new Mock<ITelegramBotClientWrapper>();

    public IModerationService CreateModerationServiceWithFake()
    {
        return new Mock<IModerationService>().Object;
    }

            public CaptchaService CreateCaptchaServiceWithFake()
        {
            return new CaptchaService(
                new Mock<ITelegramBotClientWrapper>().Object,
                new Mock<ILogger<CaptchaService>>().Object,
                new Mock<IMessageService>().Object,
                AppConfigTestFactory.CreateDefault(),
                new Mock<IViolationTracker>().Object,
                new Mock<IUserBanService>().Object
            );
        }

    public async Task<StatisticsService> CreateAsync()
    {
        return await Task.FromResult(CreateStatisticsService());
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        return new SpamHamClassifier(
            new Mock<ILogger<SpamHamClassifier>>().Object
        );
    }
    #endregion
}
