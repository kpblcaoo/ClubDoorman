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
        Mock<IUserManager>? userManagerMock = null,
        Action<Mock<IUserManager>>? configureUserManager = null,
        Action<Mock<ICommandRouter>>? configureCommandRouter = null)
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
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
        var handler = CreateHandler(Flags(basePath), configureCommandRouter: cr =>
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
        var handler = CreateHandler(Flags(basePath), userManager, um => { });

        await handler.HandleAsync(update, CancellationToken.None);

        using var doc = LoadSemanticsJson(basePath);
        var root = doc.RootElement;
        Assert.That(root.GetProperty("action").GetString(), Is.EqualTo("Delete"));
        Assert.That(root.GetProperty("ruleCode").GetString(), Is.EqualTo("Banlist"));
    }
}
