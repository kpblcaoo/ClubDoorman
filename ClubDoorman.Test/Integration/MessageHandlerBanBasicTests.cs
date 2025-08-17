using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using ClubDoorman.Test.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Features.Moderation;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Базовые тесты банов с использованием MessageHandlerTestFactory
/// Демонстрирует рефакторинг старых тестов на новую инфраструктуру
/// <tags>integration, bans, message-handler, test-kit</tags>
/// </summary>
[TestFixture]
[Category("integration")]
public class MessageHandlerBanBasicTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _handler = null!;
    private Mock<ITelegramBotClientWrapper> _botMock = null!;
    private Mock<IModerationFacade> _moderationServiceMock = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new MessageHandlerTestFactory()
            .WithStandardMocks()
            .WithBanMocks();

        _botMock = _factory.BotMock;
        _moderationServiceMock = _factory.ModerationServiceMock;

        _handler = _factory.CreateMessageHandler();
    }

    [Test]
    [Category("autofixture")]
    public async Task DeleteAndReportMessage_WhenModerationReturnsDelete_DeletesMessage()
    {
        // Arrange - используем builders для читаемого создания тестовых данных
        var user = TestKitBuilders.CreateUser()
            .WithId(123456789)
            .WithUsername("testuser")
            .AsRegularUser()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(-1001234567890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .WithText("spam")
            .FromUser(user)
            .InChat(chat)
            .Build();

        // Настраиваем модерацию через умные моки
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "ML решил что это спам"));

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert
        _botMock.Verify(x => x.DeleteMessage(chat.Id, message.MessageId, It.IsAny<CancellationToken>()), Times.Once);
    }
}