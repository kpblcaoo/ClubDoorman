using Microsoft.Extensions.Logging;

namespace ClubDoorman.Infrastructure.ErrorHandling.ErrorHandlingStrategies;

/// <summary>
/// Стратегия повторных попыток для временных ошибок
/// </summary>
public class RetryStrategy : IErrorHandlingStrategy
{
    private readonly ILogger<RetryStrategy> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;

    public string Name => "RetryStrategy";
    public int Priority => 10; // Высокий приоритет - выполняем до других стратегий

    public RetryStrategy(ILogger<RetryStrategy> logger, int maxRetries = 3, TimeSpan? baseDelay = null)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
    }

    public bool CanHandle(Exception exception, ErrorContext context)
    {
        // Повторяем только для временных ошибок
        return IsRetryableException(exception);
    }

    public async Task<ErrorHandlingResult> HandleAsync(Exception exception, ErrorContext context, CancellationToken cancellationToken = default)
    {
        // Эта стратегия не обрабатывает исключения напрямую,
        // а используется в ExecuteWithErrorHandlingAsync для retry логики
        return ErrorHandlingResult.Failure(shouldContinue: true);
    }

    /// <summary>
    /// Выполняет операцию с повторными попытками
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="context">Контекст ошибки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат операции</returns>
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, ErrorContext context, CancellationToken cancellationToken = default)
    {
        var lastException = (Exception?)null;
        
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var result = await operation();
                
                if (attempt > 1)
                {
                    _logger.LogInformation("Операция {Operation} успешно выполнена с попытки {Attempt}", 
                        context.Operation, attempt);
                }
                
                return result;
            }
            catch (Exception ex) when (IsRetryableException(ex))
            {
                lastException = ex;
                
                if (attempt < _maxRetries)
                {
                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning(ex, "Попытка {Attempt}/{MaxRetries} операции {Operation} не удалась, повтор через {Delay}ms", 
                        attempt, _maxRetries, context.Operation, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    _logger.LogError(ex, "Операция {Operation} не удалась после {MaxRetries} попыток", 
                        context.Operation, _maxRetries);
                }
            }
        }

        // Если все попытки исчерпаны, возвращаем значение по умолчанию
        return default(T)!;
    }

    /// <summary>
    /// Выполняет операцию с повторными попытками (без возвращаемого значения)
    /// </summary>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="context">Контекст ошибки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task ExecuteWithRetryAsync(Func<Task> operation, ErrorContext context, CancellationToken cancellationToken = default)
    {
        var lastException = (Exception?)null;
        
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await operation();
                
                if (attempt > 1)
                {
                    _logger.LogInformation("Операция {Operation} успешно выполнена с попытки {Attempt}", 
                        context.Operation, attempt);
                }
                
                return;
            }
            catch (Exception ex) when (IsRetryableException(ex))
            {
                lastException = ex;
                
                if (attempt < _maxRetries)
                {
                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning(ex, "Попытка {Attempt}/{MaxRetries} операции {Operation} не удалась, повтор через {Delay}ms", 
                        attempt, _maxRetries, context.Operation, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    _logger.LogError(ex, "Операция {Operation} не удалась после {MaxRetries} попыток", 
                        context.Operation, _maxRetries);
                }
            }
        }
    }

    private static bool IsRetryableException(Exception exception)
    {
        // Повторяем для временных ошибок
        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException => true,
            OperationCanceledException => false, // Не повторяем отмененные операции
            _ => exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                 exception.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                 exception.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
        };
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        // Экспоненциальная задержка с джиттером
        var delay = _baseDelay * Math.Pow(2, attempt - 1);
        var jitter = Random.Shared.NextDouble() * 0.1; // ±10% джиттер
        return TimeSpan.FromMilliseconds(delay.TotalMilliseconds * (1 + jitter));
    }
} 