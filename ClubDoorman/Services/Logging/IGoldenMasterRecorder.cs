using Telegram.Bot.Types;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// Интерфейс для записи Golden Master сэмплов вход/выход обработки сообщения.
/// Реализация должна быть дешёвой в случае отключения флагов.
/// </summary>
public interface IGoldenMasterRecorder
{
    /// <summary>
    /// Попробовать записать входное сообщение (до обработки). Возвращает correlationId если запись активна.
    /// </summary>
    string? TryRecordInput(Update update, string handlerName, long? chatId, long? userId);

    /// <summary>
    /// Попробовать записать результат после обработки (использует correlationId для пары).
    /// </summary>
    void TryRecordOutput(string? correlationId, object? resultPayload);
}
