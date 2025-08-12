using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Интерфейс для маршрутизации команд к соответствующим обработчикам
/// </summary>
public interface ICommandRouter
{
    /// <summary>
    /// Обрабатывает команду, передавая её соответствующему обработчику
    /// </summary>
    /// <param name="message">Сообщение с командой</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>true, если команда была обработана; false, если обработчик не найден</returns>
    Task<bool> HandleCommandAsync(Message message, CancellationToken cancellationToken = default);
}