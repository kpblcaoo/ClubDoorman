namespace ClubDoorman.Services.Core;

/// <summary>
/// Интерфейс для генерации случайных чисел, позволяющий мокирование в тестах
/// </summary>
public interface IRandomProvider
{
    /// <summary>
    /// Генерирует случайное число от 0 (включительно) до maxValue (исключительно)
    /// </summary>
    int Next(int maxValue);
    
    /// <summary>
    /// Генерирует случайное число в заданном диапазоне
    /// </summary>
    int Next(int minValue, int maxValue);
    
    /// <summary>
    /// Заполняет байтовый массив случайными значениями
    /// </summary>
    void NextBytes(byte[] buffer);
    
    /// <summary>
    /// Генерирует случайное число с плавающей точкой от 0.0 до 1.0
    /// </summary>
    double NextDouble();
}

/// <summary>
/// Реализация IRandomProvider по умолчанию, использующая системный Random
/// </summary>
public class SystemRandomProvider : IRandomProvider
{
    private readonly Random _random = new();
    
    public int Next(int maxValue) => _random.Next(maxValue);
    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
    public void NextBytes(byte[] buffer) => _random.NextBytes(buffer);
    public double NextDouble() => _random.NextDouble();
}