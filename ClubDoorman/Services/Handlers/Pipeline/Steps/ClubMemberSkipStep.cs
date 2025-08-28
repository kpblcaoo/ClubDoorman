using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.UserManagement;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// 140: ClubMemberSkipStep — если пользователь клубный, пропускает дальнейшую модерацию.
/// Семантика: kind=club_member_skip, action=Allow, ruleCode=ClubMemberSkip.
/// </summary>
public class ClubMemberSkipStep : IMessageStep
{
    private readonly IUserManager _userManager;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<ClubMemberSkipStep> _logger;

    public int Order => 140;
    public string Name => nameof(ClubMemberSkipStep);

    public ClubMemberSkipStep(IUserManager userManager, IModerationEventPublisher events, ILogger<ClubMemberSkipStep> logger)
    {
        _userManager = userManager;
        _events = events;
        _logger = logger;
    }

    public async Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var msg = context.Message;
        var user = msg.From;
        if (user == null || user.IsBot) return StepResult.Continue();
        if (context.UserResultHandled) return StepResult.Continue();
        var clubName = await _userManager.GetClubUsername(user.Id);
        if (string.IsNullOrEmpty(clubName)) return StepResult.Continue();
        _logger.LogDebug("[Pipeline] ClubMemberSkipStep user {UserId} recognized as club member {Club}", user.Id, clubName);
        context.ClubMemberSkipHandled = true;
        var resultObj = new { kind = "club_member_skip", action = "Allow", ruleCode = "ClubMemberSkip" };
        context.UserResult = resultObj;
        context.UserResultHandled = true;
        _events.Publish(context.GmCorrelation, new ModerationEvent("club_member_skip", Action: "Allow", RuleCode: RuleCode.ClubMemberSkip));
        return StepResult.StopOk("club-member-skip");
    }
}
