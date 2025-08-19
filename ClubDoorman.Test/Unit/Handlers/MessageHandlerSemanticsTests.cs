using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Logging;
using ClubDoorman.Models.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Features.UserJoin;
using ClubDoorman.Services.Notifications;
using ClubDoorman.Services.AI;
using ClubDoorman.Services;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Models; // ModerationResult, ModerationAction, CaptchaInfo

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Golden semantics tests: проверяем что ранние пути пишут action/ruleCode в .sem.json
/// /command -> Allow Command, banlist -> Delete Banlist
/// </summary>
[TestFixture]
[Category("golden")]
public class MessageHandlerSemanticsTests
{
    private static IOptions<LoggingFlagsOptions> Flags(string basePath) => Options.Create(new LoggingFlagsOptions
    {
        GoldenMasterEnabled = true,
        GoldenSampleRate = 1.0,
        GoldenBasePath = basePath,
    });

    private static MessageHandler CreateHandler(IOptions<LoggingFlagsOptions> flags,
        Mock<ITelegramBotClientWrapper>? botMock = null,
        Mock<IUserManager>? userManagerMock = null,
        Action<Mock<IUserManager>>? configureUserManager = null,
        Action<Mock<ICommandRouter>>? configureCommandRouter = null)
    {
        var bot = botMock ?? new Mock<ITelegramBotClientWrapper>();
        var userManager = userManagerMock ?? new Mock<IUserManager>();
        configureUserManager?.Invoke(userManager);
        var appConfig = new Mock<IAppConfig>();
        appConfig.Setup(x => x.AdminChatId).Returns(123456789L);
        appConfig.Setup(x => x.LogAdminChatId).Returns(123456789L);
    appConfig.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
        appConfig.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
        var userBanService = new Mock<IUserBanService>();
        userBanService.Setup(x => x.HandleBlacklistBanAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var channelModeration = new Mock<IChannelModerationService>();
        var commandRouter = new Mock<ICommandRouter>();
        configureCommandRouter?.Invoke(commandRouter);
        var userJoinFacade = new Mock<IUserJoinFacade>();
        var moderationFacade = new Mock<IModerationFacade>();
        moderationFacade.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>())).Returns(false);
        moderationFacade.Setup(x => x.CheckMessageAsync(It.IsAny<Message>())).ReturnsAsync(new ModerationResult(ModerationAction.Allow, "allow", 0));
        var botPermissions = new Mock<IBotPermissionsService>();
        botPermissions.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var captchaService = new Mock<ICaptchaService>();
        captchaService.Setup(x => x.GenerateKey(It.IsAny<long>(), It.IsAny<long>())).Returns("k");
    captchaService.Setup(x => x.GetCaptchaInfo(It.IsAny<string>())).Returns((CaptchaInfo?)null);
        var userFlowLogger = new Mock<IUserFlowLogger>();
        var forwarding = new Mock<IForwardingService>();
        forwarding.Setup(x => x.IsChannelDiscussion(It.IsAny<Chat>(), It.IsAny<Message>())).ReturnsAsync(false);
        var aiCascade = new Mock<IAiCascadeService>();
        aiCascade.Setup(x => x.PerformAiProfileAnalysisAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var recorder = new GoldenMasterRecorder(flags, new NullLogger<GoldenMasterRecorder>());

        return new MessageHandler(
            bot.Object,
            userManager.Object,
            appConfig.Object,
            userBanService.Object,
            channelModeration.Object,
            commandRouter.Object,
            userJoinFacade.Object,
            moderationFacade.Object,
            new NullLogger<MessageHandler>(),
            botPermissions.Object,
            captchaService.Object,
            userFlowLogger.Object,
            forwarding.Object,
            aiCascade.Object,
            recorder,
            flags);
    }

    private static (Update update, string basePath) BuildCommandUpdate(string command)
    {
        var basePath = Path.Combine(Path.GetTempPath(), "gm_semantics_" + Path.GetRandomFileName());
        Directory.CreateDirectory(basePath);
        var update = new Update
        {
            Id = 1,
            Message = new Message
            {
                Chat = new Chat { Id = -100500, Type = ChatType.Supergroup, Title = "CmdChat" },
                From = new User { Id = 777, IsBot = false, FirstName = "Admin" },
                Text = command
            }
        };
        return (update, basePath);
    }

    private static (Update update, string basePath, Mock<IUserManager> userManager) BuildBanlistUpdate()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "gm_semantics_" + Path.GetRandomFileName());
        Directory.CreateDirectory(basePath);
        var userManager = new Mock<IUserManager>();
        userManager.Setup(x => x.InBanlist(It.IsAny<long>())).ReturnsAsync(true); // trigger banlist path
        var update = new Update
        {
            Id = 2,
            Message = new Message
            {
                Chat = new Chat { Id = -100600, Type = ChatType.Supergroup, Title = "BanChat" },
                From = new User { Id = 888, IsBot = false, FirstName = "Spammer" },
                Text = "spam text"
            }
        };
        return (update, basePath, userManager);
    }

    private static JsonDocument LoadSemanticsJson(string basePath)
    {
        var todayDir = Path.Combine(basePath, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        Assert.That(Directory.Exists(todayDir), Is.True, "Golden day directory missing");
        var semFile = Directory.GetFiles(todayDir, "*.sem.json").OrderBy(f => f).LastOrDefault();
        Assert.That(semFile, Is.Not.Null, "Semantics file not found");
        var json = File.ReadAllText(semFile!);
        return JsonDocument.Parse(json);
    }

    [Test]
    public async Task Command_EarlyExit_WritesAllowCommandSemantics()
    {
        var (update, basePath) = BuildCommandUpdate("/start");
    var handler = CreateHandler(Flags(basePath), botMock: null, userManagerMock: null, configureCommandRouter: cr =>
            cr.Setup(x => x.HandleCommandAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>())).ReturnsAsync(true));

        await handler.HandleAsync(update, CancellationToken.None);

        using var doc = LoadSemanticsJson(basePath);
        var root = doc.RootElement;
        Assert.That(root.GetProperty("action").GetString(), Is.EqualTo("Allow"));
        Assert.That(root.GetProperty("ruleCode").GetString(), Is.EqualTo("Command"));
    }

    [Test]
    public async Task Banlist_Hit_WritesDeleteBanlistSemantics()
    {
        var (update, basePath, userManager) = BuildBanlistUpdate();
    var handler = CreateHandler(Flags(basePath), botMock: null, userManagerMock: userManager, configureUserManager: um => { });

        await handler.HandleAsync(update, CancellationToken.None);

        using var doc = LoadSemanticsJson(basePath);
        var root = doc.RootElement;
        Assert.That(root.GetProperty("action").GetString(), Is.EqualTo("Delete"));
        Assert.That(root.GetProperty("ruleCode").GetString(), Is.EqualTo("Banlist"));
    }

    [Test]
    public async Task PrivateChat_NonCommand_SkipsAndWritesSemanticRule()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "gm_semantics_" + Path.GetRandomFileName());
        Directory.CreateDirectory(basePath);
        var flags = Flags(basePath);
        var update = new Update
        {
            Id = 3,
            Message = new Message
            {
                Chat = new Chat { Id = 999001, Type = ChatType.Private, FirstName = "User" },
                From = new User { Id = 424242, IsBot = false, FirstName = "User" },
                Text = "hello there"
            }
        };
    var handler = CreateHandler(flags);
        await handler.HandleAsync(update, CancellationToken.None);
        using var doc = LoadSemanticsJson(basePath);
        var root = doc.RootElement;
    // private_skip path only emits ruleCode via ReasonCodeMapper
    Assert.That(root.TryGetProperty("action", out var actionEl), Is.True, "Semantics JSON should include action key (null)");
    Assert.That(actionEl.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Null));
    var rule = root.GetProperty("ruleCode").GetString();
    Assert.That(rule, Is.EqualTo("PrivateSkip"));
    }

    [Test]
    public async Task NewMembers_TriggersSemanticRule()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "gm_semantics_" + Path.GetRandomFileName());
        Directory.CreateDirectory(basePath);
        var flags = Flags(basePath);
        var update = new Update
        {
            Id = 4,
            Message = new Message
            {
                Chat = new Chat { Id = -100777, Type = ChatType.Supergroup, Title = "JoinChat" },
                From = new User { Id = 111, IsBot = false, FirstName = "Starter" },
                NewChatMembers = new[] { new User { Id = 222, IsBot = false, FirstName = "Newbie" } }
            }
        };
    var handler = CreateHandler(flags);
        await handler.HandleAsync(update, CancellationToken.None);
        using var doc = LoadSemanticsJson(basePath);
        var root = doc.RootElement;
    Assert.That(root.TryGetProperty("action", out var actionEl), Is.True);
    Assert.That(actionEl.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Null));
    var rule = root.GetProperty("ruleCode").GetString();
    Assert.That(rule, Is.EqualTo("NewMembers"));
    }

    [Test]
    public async Task LeftMemberCleanup_EmitsSemanticRule()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "gm_semantics_" + Path.GetRandomFileName());
        Directory.CreateDirectory(basePath);
        var flags = Flags(basePath);
        var bot = new User { Id = 515151, IsBot = true, FirstName = "Bot" };
        var update = new Update
        {
            Id = 5,
            Message = new Message
            {
                Chat = new Chat { Id = -100888, Type = ChatType.Supergroup, Title = "LeaveChat" },
                From = bot, // simulate message from bot about left member
                LeftChatMember = new User { Id = 999, IsBot = false, FirstName = "Leaver" }
            }
        };
        var botMock = new Mock<ITelegramBotClientWrapper>();
        botMock.SetupGet(x => x.BotId).Returns(515151);
    var handler = CreateHandler(flags, botMock: botMock);
        // Need to set BotId on ITelegramBotClientWrapper mock used inside handler -> simplest: ignore delete path (won't throw) and rely on semantics kind
        await handler.HandleAsync(update, CancellationToken.None);
        using var doc = LoadSemanticsJson(basePath);
        var root = doc.RootElement;
    Assert.That(root.TryGetProperty("action", out var actionEl), Is.True);
    Assert.That(actionEl.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Null));
    var rule = root.GetProperty("ruleCode").GetString();
    Assert.That(rule, Is.EqualTo("LeftMemberCleanup"));
    }

    [Test]
    public async Task ChannelMessage_EmitsSemanticRule()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "gm_semantics_" + Path.GetRandomFileName());
        Directory.CreateDirectory(basePath);
        var flags = Flags(basePath);
        var update = new Update
        {
            Id = 6,
            Message = new Message
            {
                Chat = new Chat { Id = -100999, Type = ChatType.Supergroup, Title = "ChannelChat" },
                From = new User { Id = 333, IsBot = false, FirstName = "Poster" },
                SenderChat = new Chat { Id = -200001, Type = ChatType.Channel, Title = "ChannelName" },
                Text = "Channel forwarded content"
            }
        };
    var handler = CreateHandler(flags);
        await handler.HandleAsync(update, CancellationToken.None);
        using var doc = LoadSemanticsJson(basePath);
        var root = doc.RootElement;
    Assert.That(root.TryGetProperty("action", out var actionEl), Is.True);
    Assert.That(actionEl.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Null));
    var rule = root.GetProperty("ruleCode").GetString();
    Assert.That(rule, Is.EqualTo("ChannelMessage"));
    }
}
