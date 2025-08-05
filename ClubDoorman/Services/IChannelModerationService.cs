using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для модерации каналов
/// <tags>channel, moderation, proxy</tags>
/// </summary>
public interface IChannelModerationService
{
    /// <summary>
    /// Обрабатывает сообщение от канала
    /// <tags>channel, moderation, proxy</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleChannelMessageAsync(Message message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Проверяет, является ли отправитель владельцем канала
    /// <tags>channel, owner, validation</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если отправитель является владельцем канала</returns>
    Task<bool> IsChannelOwnerAsync(Message message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Проверяет, является ли чат обсуждением данного канала
    /// <tags>channel, discussion, linked</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если чат является обсуждением канала</returns>
    Task<bool> IsChannelDiscussionAsync(Message message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Проверяет, следует ли разрешить сообщение от канала без модерации
    /// <tags>channel, moderation, allow</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если сообщение следует разрешить без модерации</returns>
    Task<bool> ShouldAllowChannelMessageAsync(Message message, CancellationToken cancellationToken = default);
} 