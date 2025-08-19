namespace ClubDoorman.Services.Handlers.Pipeline;

/// <summary>
/// Шаг обработки входящего сообщения (pipeline). Минимизирует логику в монолитном MessageHandler.
/// </summary>
public interface IMessageStep
{
    /// <summary>Порядок выполнения (чем меньше число, тем раньше запускается).</summary>
    int Order { get; }

    /// <summary>Человекочитаемое имя шага для логов/метрик.</summary>
    string Name { get; }

    /// <summary>
    /// Выполняет шаг. Возвращает результат, который решает продолжать ли конвейер.
    /// Обязан не кидать наружу исключения: в случае внутренних ошибок вернуть StepResult.Fail(ex).
    /// </summary>
    Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken);
}
