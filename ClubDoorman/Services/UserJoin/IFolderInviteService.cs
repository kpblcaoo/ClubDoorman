using Telegram.Bot.Types;

namespace ClubDoorman.Services.UserJoin;

/// <summary>
/// Сервис для обработки пользователей, входящих через папки
/// <tags>user-join, folder-invite, moderation, ban</tags>
/// </summary>
public interface IFolderInviteService
{
    /// <summary>
    /// Проверяет, вошел ли пользователь через папку и обрабатывает это событие
    /// </summary>
    /// <param name="chatMemberUpdated">Обновление участника чата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если пользователь был забанен за вход через папку</returns>
    Task<bool> HandleFolderInviteAsync(ChatMemberUpdated chatMemberUpdated, CancellationToken cancellationToken = default);
}
