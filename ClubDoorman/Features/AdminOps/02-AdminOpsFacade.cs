using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Фасад для админских операций - единая точка входа для команд /spam, /ham, /check, /stats, /say
/// </summary>
public class AdminOpsFacade : IAdminOpsFacade
{
    private readonly ICommandRouter _commandRouter;
    private readonly ILogger<AdminOpsFacade> _logger;

    public AdminOpsFacade(
        ICommandRouter commandRouter,
        ILogger<AdminOpsFacade> logger)
    {
        _commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Обрабатывает админскую команду через CommandRouter
    /// </summary>
    /// <param name="message">Сообщение с командой</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если команда была обработана</returns>
    public async Task<bool> HandleAdminCommandAsync(Message message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AdminOpsFacade: обработка команды '{Command}' от пользователя {UserId} в чате {ChatId}",
            message.Text?.Split(' ')[0], message.From?.Id, message.Chat?.Id);

        var handled = await _commandRouter.HandleCommandAsync(message, cancellationToken);

        if (handled)
        {
            _logger.LogDebug("AdminOpsFacade: команда '{Command}' успешно обработана",
                message.Text?.Split(' ')[0]);
        }
        else
        {
            _logger.LogDebug("AdminOpsFacade: команда '{Command}' не была обработана",
                message.Text?.Split(' ')[0]);
        }

        return handled;
    }
}
