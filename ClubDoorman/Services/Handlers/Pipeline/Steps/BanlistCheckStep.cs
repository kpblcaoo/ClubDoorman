using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserManagement;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// 110: BanlistCheckStep — банит пользователя если он в чёрном списке.
/// Семантика: kind=banlist_ban, action=Delete, ruleCode=Banlist.
/// </summary>
public class BanlistCheckStep : IMessageStep
{
    private readonly IUserManager _userManager;
    private readonly IUserBanService _userBanService;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<BanlistCheckStep> _logger;

    public int Order => 110;
    public string Name => nameof(BanlistCheckStep);

    public BanlistCheckStep(IUserManager userManager, IUserBanService userBanService, IModerationEventPublisher events, ILogger<BanlistCheckStep> logger)
    {
        _userManager = userManager;
        _userBanService = userBanService;
        _events = events;
        _logger = logger;
    }

    public async Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var msg = context.Message;
        var user = msg.From;
        if (user == null || user.IsBot) return StepResult.Continue();
        if (!await _userManager.InBanlist(user.Id)) return StepResult.Continue();
        _logger.LogDebug("[Pipeline] BanlistCheckStep banning user {UserId} in chat {ChatId}", user.Id, msg.Chat.Id);
        try { await _userBanService.HandleBlacklistBanAsync(msg, user, msg.Chat, cancellationToken); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BanlistCheckStep failed HandleBlacklistBanAsync user {UserId}", user.Id);
            return StepResult.Fail(ex, "banlist-exception");
        }
        context.BanlistHandled = true;
        var resultObj = new { kind = "banlist_ban", action = "Delete", ruleCode = "Banlist" };
        context.UserResult = resultObj;
        context.UserResultHandled = true;
        _events.Publish(context.GmCorrelation, new ModerationEvent("banlist_ban", Action: "Delete", RuleCode: RuleCode.Banlist));
        return StepResult.StopOk("banlist");
    }
}
