using Telegram.Bot.Types;

namespace ClubDoorman.Infrastructure.ErrorHandling;

/// <summary>
/// Контекст ошибки для передачи дополнительной информации
/// </summary>
public class ErrorContext
{
    /// <summary>
    /// Название операции, в которой произошла ошибка
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Дополнительное описание контекста
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Пользователь, связанный с ошибкой
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Чат, связанный с ошибкой
    /// </summary>
    public Chat? Chat { get; set; }

    /// <summary>
    /// Сообщение, связанное с ошибкой
    /// </summary>
    public Message? Message { get; set; }

    /// <summary>
    /// Дополнительные данные для логирования
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    /// <summary>
    /// Уровень критичности ошибки
    /// </summary>
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Medium;

    /// <summary>
    /// Создает новый контекст ошибки
    /// </summary>
    /// <param name="operation">Название операции</param>
    /// <param name="description">Описание контекста</param>
    /// <param name="severity">Уровень критичности</param>
    public ErrorContext(string operation, string? description = null, ErrorSeverity severity = ErrorSeverity.Medium)
    {
        Operation = operation;
        Description = description;
        Severity = severity;
    }

    /// <summary>
    /// Создает контекст ошибки для пользовательской операции
    /// </summary>
    /// <param name="operation">Название операции</param>
    /// <param name="user">Пользователь</param>
    /// <param name="chat">Чат</param>
    /// <param name="description">Описание контекста</param>
    /// <param name="severity">Уровень критичности</param>
    public ErrorContext(string operation, User user, Chat chat, string? description = null, ErrorSeverity severity = ErrorSeverity.Medium)
        : this(operation, description, severity)
    {
        User = user;
        Chat = chat;
    }

    /// <summary>
    /// Создает контекст ошибки для операции с сообщением
    /// </summary>
    /// <param name="operation">Название операции</param>
    /// <param name="message">Сообщение</param>
    /// <param name="description">Описание контекста</param>
    /// <param name="severity">Уровень критичности</param>
    public ErrorContext(string operation, Message message, string? description = null, ErrorSeverity severity = ErrorSeverity.Medium)
        : this(operation, description, severity)
    {
        Message = message;
        User = message.From;
        Chat = message.Chat;
    }

    /// <summary>
    /// Добавляет дополнительные данные в контекст
    /// </summary>
    /// <param name="key">Ключ</param>
    /// <param name="value">Значение</param>
    /// <returns>Текущий контекст для цепочки вызовов</returns>
    public ErrorContext WithData(string key, object value)
    {
        AdditionalData[key] = value;
        return this;
    }

    /// <summary>
    /// Устанавливает уровень критичности
    /// </summary>
    /// <param name="severity">Уровень критичности</param>
    /// <returns>Текущий контекст для цепочки вызовов</returns>
    public ErrorContext WithSeverity(ErrorSeverity severity)
    {
        Severity = severity;
        return this;
    }
}

/// <summary>
/// Уровень критичности ошибки
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Низкая критичность - не влияет на работу системы
    /// </summary>
    Low = 0,

    /// <summary>
    /// Средняя критичность - может влиять на отдельные функции
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Высокая критичность - влияет на основные функции
    /// </summary>
    High = 2,

    /// <summary>
    /// Критическая ошибка - может привести к остановке системы
    /// </summary>
    Critical = 3
} 