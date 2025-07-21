using ClubDoorman.Infrastructure.ErrorHandling.ErrorHandlingStrategies;

namespace ClubDoorman.Infrastructure.ErrorHandling;

/// <summary>
/// Интерфейс для централизованной обработки ошибок
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Обрабатывает исключение с указанным контекстом
    /// </summary>
    /// <param name="exception">Исключение для обработки</param>
    /// <param name="context">Контекст ошибки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleAsync(Exception exception, ErrorContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет операцию с обработкой ошибок
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="context">Контекст ошибки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат операции или значение по умолчанию при ошибке</returns>
    Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, ErrorContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет операцию с обработкой ошибок (без возвращаемого значения)
    /// </summary>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="context">Контекст ошибки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task ExecuteWithErrorHandlingAsync(Func<Task> operation, ErrorContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Регистрирует стратегию обработки для определенного типа исключения
    /// </summary>
    /// <param name="exceptionType">Тип исключения</param>
    /// <param name="strategy">Стратегия обработки</param>
    void RegisterStrategy(Type exceptionType, IErrorHandlingStrategy strategy);

    /// <summary>
    /// Регистрирует стратегию обработки для определенного типа исключения
    /// </summary>
    /// <typeparam name="TException">Тип исключения</typeparam>
    /// <param name="strategy">Стратегия обработки</param>
    void RegisterStrategy<TException>(IErrorHandlingStrategy strategy) where TException : Exception;
} 