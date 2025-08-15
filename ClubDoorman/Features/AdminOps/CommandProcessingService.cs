using ClubDoorman.Services.Handlers;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;


namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Сервис для обработки команд
/// <tags>commands, processing, proxy</tags>
/// </summary>
public class CommandProcessingService : ICommandProcessingService
{
    private readonly ICommandRouter _commandRouter;
    private readonly ILogger<CommandProcessingService> _logger;

    /// <summary>
    /// Создает экземпляр CommandProcessingService
    /// <tags>commands, constructor, dependency-injection</tags>
    /// </summary>
    /// <param name="commandRouter">Обработчик сообщений</param>
    /// <param name="logger">Логгер</param>
    public CommandProcessingService(ICommandRouter commandRouter, ILogger<CommandProcessingService> logger)
    {
        _commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Обрабатывает команду
    /// <tags>commands, processing, proxy</tags>
    /// </summary>
    /// <param name="message">Сообщение с командой</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task HandleCommandAsync(Message message, CancellationToken cancellationToken = default)
    {
        var handled = await _commandRouter.HandleCommandAsync(message, cancellationToken);

        Console.WriteLine($"[DEBUG] MessageHandler.HandleCommandAsync: CommandRouter returned {handled}");
    }
}