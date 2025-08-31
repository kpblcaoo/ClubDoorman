namespace ClubDoorman.Services.Handlers.Pipeline;

public interface IMessagePipeline
{
    Task RunAsync(MessageContext ctx, CancellationToken cancellationToken);
}
