using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
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
/// TestFactory для TelegramBotClientWrapper
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class TelegramBotClientWrapperTestFactory
{

    public TelegramBotClientWrapper CreateTelegramBotClientWrapper()
    {
        return new TelegramBotClientWrapper(
            new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"),
            NullLogger<TelegramBotClientWrapper>.Instance
        );
    }

    #region Configuration Methods

    #endregion

    #region Smart Methods Based on Business Logic

    public FakeTelegramClient FakeTelegramClient => FakeTelegramClientFactory.Create();

    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock => new Mock<ITelegramBotClientWrapper>();

    public ModerationServiceAdapter CreateModerationServiceWithFake()
    {
        return new ModerationServiceAdapter(
            new Mock<IModerationPolicy>().Object
        );
    }

    public IUserManager CreateUserManagerWithFake()
    {
        return new Mock<IUserManager>().Object;
    }

    public async Task<TelegramBotClientWrapper> CreateAsync()
    {
        return await Task.FromResult(CreateTelegramBotClientWrapper());
    }
    #endregion
}
