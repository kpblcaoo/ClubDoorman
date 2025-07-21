using Telegram.Bot.Types;

namespace ClubDoorman.Infrastructure.ErrorHandling;

/// <summary>
/// Интерфейс для middleware автоматической обработки ошибок Telegram API
/// </summary>
public interface IErrorHandlingMiddleware
{
    /// <summary>
    /// Выполняет операцию Telegram API с автоматической обработкой ошибок
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="operationName">Название операции</param>
    /// <param name="user">Пользователь (опционально)</param>
    /// <param name="chat">Чат (опционально)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат операции или значение по умолчанию</returns>
    Task<T> ExecuteTelegramApiAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        User? user = null,
        Chat? chat = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет операцию Telegram API с автоматической обработкой ошибок (без возвращаемого значения)
    /// </summary>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="operationName">Название операции</param>
    /// <param name="user">Пользователь (опционально)</param>
    /// <param name="chat">Чат (опционально)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task ExecuteTelegramApiAsync(
        Func<Task> operation,
        string operationName,
        User? user = null,
        Chat? chat = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет операцию с сообщением с автоматической обработкой ошибок
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="operationName">Название операции</param>
    /// <param name="message">Сообщение</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат операции или значение по умолчанию</returns>
    Task<T> ExecuteWithMessageAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет операцию с сообщением с автоматической обработкой ошибок (без возвращаемого значения)
    /// </summary>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="operationName">Название операции</param>
    /// <param name="message">Сообщение</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task ExecuteWithMessageAsync(
        Func<Task> operation,
        string operationName,
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обрабатывает исключение напрямую
    /// </summary>
    /// <param name="exception">Исключение для обработки</param>
    /// <param name="operationName">Название операции</param>
    /// <param name="description">Описание (опционально)</param>
    /// <param name="severity">Серьезность ошибки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleExceptionAsync(
        Exception exception,
        string operationName,
        string? description = null,
        ErrorSeverity severity = ErrorSeverity.Medium,
        CancellationToken cancellationToken = default);
} 