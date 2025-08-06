using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Handlers;
using ClubDoorman.TestInfrastructure;
using NUnit.Framework;
using ClubDoorman.Test.TestKit;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test;

/// <summary>
/// Демонстрационные тесты для показа возможностей TestKit и TestKitBogus
/// </summary>
[TestFixture]
[Category("demo")]
public class TestKitDemoTests
{
    [Test]
    public void TestKit_CreateMessageHandlerWithDefaults_WorksCorrectly()
    {
        // Arrange & Act
        var handler = TK.CreateMessageHandlerWithDefaults();

        // Assert
        Assert.That(handler, Is.Not.Null);
        Assert.That(handler, Is.InstanceOf<MessageHandler>());
    }

    [Test]
    public void TestKit_CreateMessageHandlerWithFake_WorksCorrectly()
    {
        // Arrange & Act
        var handler = TK.CreateMessageHandlerWithFake();

        // Assert
        Assert.That(handler, Is.Not.Null);
        Assert.That(handler, Is.InstanceOf<MessageHandler>());
    }

    [Test]
    public void TestKit_CreateValidMessage_WorksCorrectly()
    {
        // Arrange & Act
        var message = TK.CreateValidMessage();

        // Assert
        Assert.That(message, Is.Not.Null);
        Assert.That(message.Text, Is.EqualTo("Hello, this is a valid message!"));
        Assert.That(message.From, Is.Not.Null);
        Assert.That(message.Chat, Is.Not.Null);
    }

    [Test]
    public void TestKitBogus_CreateRealisticUser_WorksCorrectly()
    {
        // Arrange & Act
        var user = TestKitBogus.CreateRealisticUser();

        // Assert
        Assert.That(user, Is.Not.Null);
        Assert.That(user.Id, Is.GreaterThan(0));
        Assert.That(user.FirstName, Is.Not.Null.And.Not.Empty);
        Assert.That(user.IsBot, Is.False);
    }

    [Test]
    public void TestKitBogus_CreateRealisticBot_WorksCorrectly()
    {
        // Arrange & Act
        var bot = TestKitBogus.CreateRealisticBot();

        // Assert
        Assert.That(bot, Is.Not.Null);
        Assert.That(bot.Id, Is.GreaterThan(0));
        Assert.That(bot.FirstName, Is.Not.Null.And.Not.Empty);
        Assert.That(bot.IsBot, Is.True);
    }

    [Test]
    public void TestKitBogus_CreateRealisticGroup_WorksCorrectly()
    {
        // Arrange & Act
        var group = TestKitBogus.CreateRealisticGroup();

        // Assert
        Assert.That(group, Is.Not.Null);
        Assert.That(group.Id, Is.LessThan(0)); // Группы имеют отрицательные ID
        Assert.That(group.Title, Is.Not.Null.And.Not.Empty);
        Assert.That(group.Type, Is.EqualTo(Telegram.Bot.Types.Enums.ChatType.Group));
    }

    [Test]
    public void TestKitBogus_CreateRealisticMessage_WorksCorrectly()
    {
        // Arrange & Act
        var message = TestKitBogus.CreateRealisticMessage();

        // Assert
        Assert.That(message, Is.Not.Null);
        Assert.That(message.Text, Is.Not.Null.And.Not.Empty);
        Assert.That(message.From, Is.Not.Null);
        Assert.That(message.Chat, Is.Not.Null);
        // MessageId в Telegram.Bot.Types.Message является readonly и по умолчанию равен 0
        // Assert.That(message.MessageId, Is.GreaterThan(0));
    }

    [Test]
    public void TestKitBogus_CreateRealisticSpamMessage_WorksCorrectly()
    {
        // Arrange & Act
        var spamMessage = TestKitBogus.CreateRealisticSpamMessage();

        // Assert
        Assert.That(spamMessage, Is.Not.Null);
        Assert.That(spamMessage.Text, Is.Not.Null.And.Not.Empty);
        Assert.That(spamMessage.Text, Does.Contain("🔥").Or.Contain("💰").Or.Contain("🎁").Or.Contain("⚡").Or.Contain("💎").Or.Contain("🚀").Or.Contain("📱").Or.Contain("❗️").Or.Contain("ВНИМАНИЕ").Or.Contain("ЗАРАБОТАЛ"));
    }

    [Test]
    public void TestKit_FactoryMethods_WorkCorrectly()
    {
        // Arrange & Act
        var messageHandlerFactory = TK.CreateMessageHandlerFactory();
        var moderationServiceFactory = TK.CreateModerationServiceFactory();
        var captchaServiceFactory = TK.CreateCaptchaServiceFactory();

        // Assert
        Assert.That(messageHandlerFactory, Is.Not.Null);
        Assert.That(moderationServiceFactory, Is.Not.Null);
        Assert.That(captchaServiceFactory, Is.Not.Null);
    }

    [Test]
    public void TestKit_LegacyTestData_WorksCorrectly()
    {
        // Arrange & Act
        var message = TK.CreateValidMessage();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();

        // Assert
        Assert.That(message, Is.Not.Null);
        Assert.That(user, Is.Not.Null);
        Assert.That(chat, Is.Not.Null);
    }

    [Test]
    public void TestKitDemo_NewSpecializedStructure_WorksCorrectly()
    {
        // Демонстрация новой структуры Specialized
        var captcha = TK.Specialized.Captcha.Bait();
        var moderationResult = TK.Specialized.Moderation.Ban();
        var adminCallback = TK.Specialized.Admin.ApproveCallback();
        var memberUpdate = TK.Specialized.Updates.MemberJoined();
        var callback = TK.Specialized.Callbacks.Valid();
        var suspiciousMessage = TK.Specialized.Messages.SuspiciousUser();
        var baitUser = TK.Specialized.Users.Bait();

        // Проверяем, что все объекты созданы корректно
        Assert.That(captcha, Is.Not.Null);
        Assert.That(moderationResult, Is.Not.Null);
        Assert.That(adminCallback, Is.Not.Null);
        Assert.That(memberUpdate, Is.Not.Null);
        Assert.That(callback, Is.Not.Null);
        Assert.That(suspiciousMessage, Is.Not.Null);
        Assert.That(baitUser, Is.Not.Null);
    }

    [Test]
    public void TestKitDemo_MessageHandlerScenario_WorksCorrectly()
    {
        // Демонстрация сценария для MessageHandler
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        
        // Настройка сообщения
        message.From = user;
        message.Chat = chat;
        message.Text = "Hello, this is a test message";

        Assert.That(message.From.Id, Is.EqualTo(user.Id));
        Assert.That(message.Chat.Id, Is.EqualTo(chat.Id));
        Assert.That(message.Text, Is.EqualTo("Hello, this is a test message"));
    }

    [Test]
    public void TestKitDemo_CaptchaScenario_WorksCorrectly()
    {
        // Демонстрация сценария для капчи
        var captcha = TK.Specialized.Captcha.Bait();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        
        // Проверяем, что капча создана корректно
        Assert.That(captcha, Is.Not.Null);
        Assert.That(user, Is.Not.Null);
        Assert.That(chat, Is.Not.Null);
    }

    [Test]
    public void TestKitDemo_ModerationScenario_WorksCorrectly()
    {
        // Демонстрация сценария для модерации
        var allowResult = TK.Specialized.Moderation.Allow();
        var deleteResult = TK.Specialized.Moderation.Delete();
        var banResult = TK.Specialized.Moderation.Ban();
        
        Assert.That(allowResult, Is.Not.Null);
        Assert.That(deleteResult, Is.Not.Null);
        Assert.That(banResult, Is.Not.Null);
    }

    [Test]
    public void TestKitDemo_AdminScenario_WorksCorrectly()
    {
        // Демонстрация сценария для админских действий
        var approveCallback = TK.Specialized.Admin.ApproveCallback();
        var banCallback = TK.Specialized.Admin.BanCallback();
        var notification = TK.Specialized.Admin.Notification();
        
        Assert.That(approveCallback.Data, Is.Not.Null);
        Assert.That(banCallback.Data, Is.Not.Null);
        Assert.That(notification.Text, Is.Not.Null);
    }

    [Test]
    public void TestKitDemo_ChatUpdatesScenario_WorksCorrectly()
    {
        // Демонстрация сценария для обновлений чата
        var memberJoined = TK.Specialized.Updates.MemberJoined();
        var memberBanned = TK.Specialized.Updates.MemberBanned();
        var memberLeft = TK.Specialized.Updates.MemberLeft();
        
        Assert.That(memberJoined.NewChatMember, Is.Not.Null);
        Assert.That(memberBanned.NewChatMember, Is.Not.Null);
        Assert.That(memberLeft.NewChatMember, Is.Not.Null);
    }
} 