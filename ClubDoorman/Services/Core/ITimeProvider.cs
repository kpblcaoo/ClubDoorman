namespace ClubDoorman.Services.Core;

/// <summary>
/// Интерфейс для предоставления времени, позволяющий мокирование в тестах
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Получить текущее время UTC
    /// </summary>
    DateTime UtcNow { get; }
    
    /// <summary>
    /// Получить текущее локальное время
    /// </summary>
    DateTime Now { get; }
}

/// <summary>
/// Реализация ITimeProvider по умолчанию, использующая системное время
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
}