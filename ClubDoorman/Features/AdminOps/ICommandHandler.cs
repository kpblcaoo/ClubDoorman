using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Интерфейс для обработки команд
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// Имя команды (без /)
    /// </summary>
    string CommandName { get; }

    /// <summary>
    /// Обрабатывает команду
    /// </summary>
    Task HandleAsync(Message message, CancellationToken cancellationToken = default);
}