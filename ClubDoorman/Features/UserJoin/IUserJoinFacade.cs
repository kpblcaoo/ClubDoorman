using Telegram.Bot.Types;

namespace ClubDoorman.Features.UserJoin;

/// <summary>
/// Интерфейс фасада для функциональности присоединения пользователей
/// <tags>user-join, facade, interface, new-members, coordination</tags>
/// </summary>
public interface IUserJoinFacade
{
    /// <summary>
    /// Обрабатывает присоединение новых пользователей
    /// <tags>user-join, new-members, processing</tags>
    /// </summary>
    /// <param name="message">Сообщение о новых участниках</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleNewMembersAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обрабатывает одного нового пользователя
    /// <tags>user-join, single-user, processing</tags>
    /// </summary>
    /// <param name="userJoinMessage">Сообщение о присоединении</param>
    /// <param name="user">Пользователь</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task ProcessNewUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken = default);
}
