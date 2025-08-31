using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Интерфейс для фасада админских операций
/// </summary>
public interface IAdminOpsFacade
{
    /// <summary>
    /// Обрабатывает админскую команду
    /// </summary>
    /// <param name="message">Сообщение с командой</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если команда была обработана</returns>
    Task<bool> HandleAdminCommandAsync(Message message, CancellationToken cancellationToken = default);
}
