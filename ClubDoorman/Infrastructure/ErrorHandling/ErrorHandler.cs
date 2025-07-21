using ClubDoorman.Infrastructure.ErrorHandling.ErrorHandlingStrategies;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Infrastructure.ErrorHandling;

/// <summary>
/// Основная реализация централизованного обработчика ошибок
/// </summary>
public class ErrorHandler : IErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;
    private readonly RetryStrategy _retryStrategy;
    private readonly Dictionary<Type, IErrorHandlingStrategy> _strategies = new();
    private readonly List<IErrorHandlingStrategy> _globalStrategies = new();

    public ErrorHandler(
        ILogger<ErrorHandler> logger,
        RetryStrategy retryStrategy,
        LoggingStrategy loggingStrategy,
        NotificationStrategy notificationStrategy)
    {
        _logger = logger;
        _retryStrategy = retryStrategy;
        
        // Регистрируем глобальные стратегии
        _globalStrategies.Add(loggingStrategy);
        _globalStrategies.Add(notificationStrategy);
        
        // Сортируем по приоритету
        _globalStrategies.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public async Task HandleAsync(Exception exception, ErrorContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Начинаем обработку ошибки: {ExceptionType} в операции {Operation}", 
                exception.GetType().Name, context.Operation);

            // Сначала пробуем специфичные стратегии
            if (_strategies.TryGetValue(exception.GetType(), out var specificStrategy))
            {
                var result = await specificStrategy.HandleAsync(exception, context, cancellationToken);
                if (result.IsHandled && !result.ShouldContinue)
                {
                    _logger.LogDebug("Ошибка обработана специфичной стратегией {StrategyName}", specificStrategy.Name);
                    return;
                }
            }

            // Затем применяем глобальные стратегии
            foreach (var strategy in _globalStrategies)
            {
                if (strategy.CanHandle(exception, context))
                {
                    var result = await strategy.HandleAsync(exception, context, cancellationToken);
                    if (result.IsHandled && !result.ShouldContinue)
                    {
                        _logger.LogDebug("Ошибка обработана глобальной стратегией {StrategyName}", strategy.Name);
                        break;
                    }
                }
            }

            _logger.LogDebug("Обработка ошибки завершена");
        }
        catch (Exception handlerException)
        {
            _logger.LogError(handlerException, "Ошибка в самом обработчике ошибок при обработке {ExceptionType}", 
                exception.GetType().Name);
        }
    }

    public async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, ErrorContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем, нужен ли retry для этой операции
            if (ShouldUseRetry(context))
            {
                return await _retryStrategy.ExecuteWithRetryAsync(operation, context, cancellationToken);
            }

            // Выполняем операцию без retry
            return await operation();
        }
        catch (OperationCanceledException)
        {
            // Пробрасываем OperationCanceledException дальше для корректного завершения приложения
            throw;
        }
        catch (Exception exception)
        {
            await HandleAsync(exception, context, cancellationToken);
            
            // Возвращаем значение по умолчанию
            return default(T)!;
        }
    }

    public async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, ErrorContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем, нужен ли retry для этой операции
            if (ShouldUseRetry(context))
            {
                await _retryStrategy.ExecuteWithRetryAsync(operation, context, cancellationToken);
                return;
            }

            // Выполняем операцию без retry
            await operation();
        }
        catch (OperationCanceledException)
        {
            // Пробрасываем OperationCanceledException дальше для корректного завершения приложения
            throw;
        }
        catch (Exception exception)
        {
            await HandleAsync(exception, context, cancellationToken);
        }
    }

    public void RegisterStrategy(Type exceptionType, IErrorHandlingStrategy strategy)
    {
        if (exceptionType == null) throw new ArgumentNullException(nameof(exceptionType));
        if (strategy == null) throw new ArgumentNullException(nameof(strategy));

        _strategies[exceptionType] = strategy;
        _logger.LogDebug("Зарегистрирована стратегия {StrategyName} для типа исключения {ExceptionType}", 
            strategy.Name, exceptionType.Name);
    }

    public void RegisterStrategy<TException>(IErrorHandlingStrategy strategy) where TException : Exception
    {
        RegisterStrategy(typeof(TException), strategy);
    }

    /// <summary>
    /// Определяет, нужно ли использовать retry для данной операции
    /// </summary>
    /// <param name="context">Контекст ошибки</param>
    /// <returns>true, если нужно использовать retry</returns>
    private static bool ShouldUseRetry(ErrorContext context)
    {
        // Используем retry для операций с Telegram API и сетевых операций
        var retryableOperations = new[]
        {
            "SendMessage",
            "DeleteMessage",
            "BanChatMember",
            "UnbanChatMember",
            "RestrictChatMember",
            "GetChat",
            "GetChatMember",
            "ForwardMessage",
            "AnswerCallbackQuery",
            "HttpRequest",
            "ApiCall"
        };

        return retryableOperations.Any(op => context.Operation.Contains(op, StringComparison.OrdinalIgnoreCase));
    }
} 