namespace ClubDoorman.Services.Handlers.Pipeline;

/// <summary>
/// Нулевая реализация конвейера (используется старыми тестовыми фабриками до их миграции).
/// Не выполняет никаких шагов.
/// </summary>
internal sealed class NullMessagePipeline : IMessagePipeline
{
    public Task RunAsync(MessageContext ctx, CancellationToken cancellationToken) => Task.CompletedTask;
}
