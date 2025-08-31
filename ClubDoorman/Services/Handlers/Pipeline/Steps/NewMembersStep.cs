using ClubDoorman.Features.UserJoin;
using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

public class NewMembersStep : IMessageStep
{
    private readonly IUserJoinFacade _userJoinFacade;
    private readonly IAppConfig _appConfig;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<NewMembersStep> _logger;

    public int Order => 20;
    public string Name => nameof(NewMembersStep);

    public NewMembersStep(IUserJoinFacade userJoinFacade, IAppConfig appConfig, IModerationEventPublisher events, ILogger<NewMembersStep> logger)
    {
        _userJoinFacade = userJoinFacade;
        _appConfig = appConfig;
        _events = events;
        _logger = logger;
    }

    public async Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var msg = context.Message;
        var members = msg.NewChatMembers;
        if (members == null || members.Length == 0) return StepResult.Continue();
        if (msg.Chat.Id == _appConfig.AdminChatId) return StepResult.Continue();

        _logger.LogDebug("[Pipeline] NewMembersStep handling {Count} new member(s) in chat {ChatId}", members.Length, msg.Chat.Id);
        try
        {
            await _userJoinFacade.HandleNewMembersAsync(msg, cancellationToken);
            _events.Publish(context.GmCorrelation, new ModerationEvent("new_members", Action: null, RuleCode: RuleCode.NewMembers, Count: members.Length));
            context.NewMembersHandled = true;
            return StepResult.StopOk("new-members");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NewMembersStep failed for chat {ChatId}", msg.Chat.Id);
            return StepResult.Fail(ex, "new-members-exception");
        }
    }
}
