namespace ClubDoorman.Infrastructure.ErrorHandling.ErrorHandlingStrategies;

/// <summary>
/// Интерфейс для стратегий обработки ошибок
/// </summary>
public interface IErrorHandlingStrategy
{
    /// <summary>
    /// Название стратегии
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Определяет, может ли стратегия обработать данное исключение
    /// </summary>
    /// <param name="exception">Исключение для проверки</param>
    /// <param name="context">Контекст ошибки</param>
    /// <returns>true, если стратегия может обработать исключение</returns>
    bool CanHandle(Exception exception, ErrorContext context);

    /// <summary>
    /// Обрабатывает исключение
    /// </summary>
    /// <param name="exception">Исключение для обработки</param>
    /// <param name="context">Контекст ошибки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат обработки</returns>
    Task<ErrorHandlingResult> HandleAsync(Exception exception, ErrorContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Приоритет стратегии (меньше = выше приоритет)
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Результат обработки ошибки
/// </summary>
public class ErrorHandlingResult
{
    /// <summary>
    /// Успешно ли обработана ошибка
    /// </summary>
    public bool IsHandled { get; set; }

    /// <summary>
    /// Нужно ли продолжить обработку другими стратегиями
    /// </summary>
    public bool ShouldContinue { get; set; } = true;

    /// <summary>
    /// Дополнительные данные результата
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    /// <summary>
    /// Создает успешный результат обработки
    /// </summary>
    /// <param name="shouldContinue">Нужно ли продолжить обработку</param>
    /// <returns>Результат обработки</returns>
    public static ErrorHandlingResult Success(bool shouldContinue = true)
    {
        return new ErrorHandlingResult
        {
            IsHandled = true,
            ShouldContinue = shouldContinue
        };
    }

    /// <summary>
    /// Создает неуспешный результат обработки
    /// </summary>
    /// <param name="shouldContinue">Нужно ли продолжить обработку</param>
    /// <returns>Результат обработки</returns>
    public static ErrorHandlingResult Failure(bool shouldContinue = true)
    {
        return new ErrorHandlingResult
        {
            IsHandled = false,
            ShouldContinue = shouldContinue
        };
    }

    /// <summary>
    /// Добавляет дополнительные данные в результат
    /// </summary>
    /// <param name="key">Ключ</param>
    /// <param name="value">Значение</param>
    /// <returns>Текущий результат для цепочки вызовов</returns>
    public ErrorHandlingResult WithData(string key, object value)
    {
        AdditionalData[key] = value;
        return this;
    }
} 