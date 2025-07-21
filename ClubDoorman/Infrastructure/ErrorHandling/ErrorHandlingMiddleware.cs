using Microsoft.Extensions.Logging;

namespace ClubDoorman.Infrastructure.ErrorHandling;

/// <summary>
/// Middleware для автоматической обработки ошибок Telegram API
/// </summary>
public class ErrorHandlingMiddleware : IErrorHandlingMiddleware
{
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(IErrorHandler errorHandler, ILogger<ErrorHandlingMiddleware> logger)
    {
        _errorHandler = errorHandler;
        _logger = logger;
    }

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
    public async Task<T> ExecuteTelegramApiAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        Telegram.Bot.Types.User? user = null,
        Telegram.Bot.Types.Chat? chat = null,
        CancellationToken cancellationToken = default)
    {
        var context = new ErrorContext(operationName, "Telegram API операция", ErrorSeverity.Medium);
        
        if (user != null && chat != null)
        {
            context = new ErrorContext(operationName, user, chat, "Telegram API операция", ErrorSeverity.Medium);
        }

        return await _errorHandler.ExecuteWithErrorHandlingAsync(operation, context, cancellationToken);
    }

    /// <summary>
    /// Выполняет операцию Telegram API с автоматической обработкой ошибок (без возвращаемого значения)
    /// </summary>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="operationName">Название операции</param>
    /// <param name="user">Пользователь (опционально)</param>
    /// <param name="chat">Чат (опционально)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task ExecuteTelegramApiAsync(
        Func<Task> operation,
        string operationName,
        Telegram.Bot.Types.User? user = null,
        Telegram.Bot.Types.Chat? chat = null,
        CancellationToken cancellationToken = default)
    {
        var context = new ErrorContext(operationName, "Telegram API операция", ErrorSeverity.Medium);
        
        if (user != null && chat != null)
        {
            context = new ErrorContext(operationName, user, chat, "Telegram API операция", ErrorSeverity.Medium);
        }

        await _errorHandler.ExecuteWithErrorHandlingAsync(operation, context, cancellationToken);
    }

    /// <summary>
    /// Выполняет операцию с сообщением с автоматической обработкой ошибок
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="operationName">Название операции</param>
    /// <param name="message">Сообщение</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат операции или значение по умолчанию</returns>
    public async Task<T> ExecuteWithMessageAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        Telegram.Bot.Types.Message message,
        CancellationToken cancellationToken = default)
    {
        var context = new ErrorContext(operationName, message, "Операция с сообщением", ErrorSeverity.Medium);
        return await _errorHandler.ExecuteWithErrorHandlingAsync(operation, context, cancellationToken);
    }

    /// <summary>
    /// Выполняет операцию с сообщением с автоматической обработкой ошибок (без возвращаемого значения)
    /// </summary>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="operationName">Название операции</param>
    /// <param name="message">Сообщение</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task ExecuteWithMessageAsync(
        Func<Task> operation,
        string operationName,
        Telegram.Bot.Types.Message message,
        CancellationToken cancellationToken = default)
    {
        var context = new ErrorContext(operationName, message, "Операция с сообщением", ErrorSeverity.Medium);
        await _errorHandler.ExecuteWithErrorHandlingAsync(operation, context, cancellationToken);
    }

    /// <summary>
    /// Выполняет операцию с автоматической обработкой ошибок через IErrorHandler
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="context">Контекст ошибки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат операции или значение по умолчанию</returns>
    public async Task<T> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> operation,
        ErrorContext context,
        CancellationToken cancellationToken = default)
    {
        return await _errorHandler.ExecuteWithErrorHandlingAsync(operation, context, cancellationToken);
    }

    /// <summary>
    /// Выполняет операцию с автоматической обработкой ошибок через IErrorHandler (без возвращаемого значения)
    /// </summary>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="context">Контекст ошибки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task ExecuteWithErrorHandlingAsync(
        Func<Task> operation,
        ErrorContext context,
        CancellationToken cancellationToken = default)
    {
        await _errorHandler.ExecuteWithErrorHandlingAsync(operation, context, cancellationToken);
    }

    /// <summary>
    /// Обрабатывает исключение с дополнительным контекстом
    /// </summary>
    /// <param name="exception">Исключение для обработки</param>
    /// <param name="operationName">Название операции</param>
    /// <param name="description">Описание контекста</param>
    /// <param name="severity">Уровень критичности</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task HandleExceptionAsync(
        Exception exception,
        string operationName,
        string? description = null,
        ErrorSeverity severity = ErrorSeverity.Medium,
        CancellationToken cancellationToken = default)
    {
        var context = new ErrorContext(operationName, description, severity);
        await _errorHandler.HandleAsync(exception, context, cancellationToken);
    }
} 