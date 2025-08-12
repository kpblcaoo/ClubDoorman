using ClubDoorman.Services.Core;

namespace ClubDoorman.Test.TestKit.Fakes;

/// <summary>
/// Фальшивая реализация ITimeProvider для детерминированных тестов
/// </summary>
public class TimeProviderFake : ITimeProvider
{
    private DateTime _currentTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Получить текущее время UTC
    /// </summary>
    public DateTime UtcNow => _currentTime;

    /// <summary>
    /// Получить текущее локальное время (в тестах = UTC)
    /// </summary>
    public DateTime Now => _currentTime;

    /// <summary>
    /// Установить текущее время
    /// </summary>
    public void SetTime(DateTime time)
    {
        _currentTime = time.Kind == DateTimeKind.Utc ? time : time.ToUniversalTime();
    }

    /// <summary>
    /// Установить текущее время UTC
    /// </summary>
    public void SetUtcTime(DateTime utcTime)
    {
        _currentTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
    }

    /// <summary>
    /// Добавить время к текущему
    /// </summary>
    public void AdvanceTime(TimeSpan timeSpan)
    {
        _currentTime = _currentTime.Add(timeSpan);
    }

    /// <summary>
    /// Добавить минуты к текущему времени
    /// </summary>
    public void AdvanceMinutes(int minutes)
    {
        AdvanceTime(TimeSpan.FromMinutes(minutes));
    }

    /// <summary>
    /// Добавить часы к текущему времени
    /// </summary>
    public void AdvanceHours(int hours)
    {
        AdvanceTime(TimeSpan.FromHours(hours));
    }

    /// <summary>
    /// Добавить дни к текущему времени
    /// </summary>
    public void AdvanceDays(int days)
    {
        AdvanceTime(TimeSpan.FromDays(days));
    }

    /// <summary>
    /// Сбросить время к начальному значению
    /// </summary>
    public void Reset()
    {
        _currentTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Создать с указанным начальным временем
    /// </summary>
    public static TimeProviderFake CreateAt(DateTime startTime)
    {
        var provider = new TimeProviderFake();
        provider.SetTime(startTime);
        return provider;
    }
}

/// <summary>
/// Фальшивая реализация IRandomProvider для детерминированных тестов
/// </summary>
public class RandomProviderFake : IRandomProvider
{
    private readonly Queue<int> _intValues = new();
    private readonly Queue<double> _doubleValues = new();
    private readonly Queue<byte[]> _byteArrays = new();
    
    private int _seed = 42;

    /// <summary>
    /// Добавить заранее определенные значения для Next(maxValue)
    /// </summary>
    public RandomProviderFake WithNextValues(params int[] values)
    {
        foreach (var value in values)
            _intValues.Enqueue(value);
        return this;
    }

    /// <summary>
    /// Добавить заранее определенные значения для NextDouble()
    /// </summary>
    public RandomProviderFake WithDoubleValues(params double[] values)
    {
        foreach (var value in values)
            _doubleValues.Enqueue(value);
        return this;
    }

    /// <summary>
    /// Добавить заранее определенные байтовые массивы
    /// </summary>
    public RandomProviderFake WithByteArrays(params byte[][] arrays)
    {
        foreach (var array in arrays)
            _byteArrays.Enqueue(array);
        return this;
    }

    public int Next(int maxValue)
    {
        if (_intValues.Count > 0)
            return _intValues.Dequeue() % maxValue;
        
        // Простая псевдослучайная генерация для тестов
        _seed = (_seed * 1103515245 + 12345) & 0x7fffffff;
        return _seed % maxValue;
    }

    public int Next(int minValue, int maxValue)
    {
        var range = maxValue - minValue;
        return minValue + Next(range);
    }

    public void NextBytes(byte[] buffer)
    {
        if (_byteArrays.Count > 0)
        {
            var predefined = _byteArrays.Dequeue();
            Array.Copy(predefined, buffer, Math.Min(predefined.Length, buffer.Length));
            return;
        }

        // Заполняем предсказуемыми значениями
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)((_seed + i) % 256);
        }
    }

    public double NextDouble()
    {
        if (_doubleValues.Count > 0)
            return _doubleValues.Dequeue();
        
        // Простая псевдослучайная генерация для тестов
        _seed = (_seed * 1103515245 + 12345) & 0x7fffffff;
        return (_seed % 1000000) / 1000000.0;
    }

    /// <summary>
    /// Сбросить состояние к начальному
    /// </summary>
    public void Reset()
    {
        _intValues.Clear();
        _doubleValues.Clear();
        _byteArrays.Clear();
        _seed = 42;
    }

    /// <summary>
    /// Установить новый seed
    /// </summary>
    public void SetSeed(int seed)
    {
        _seed = seed;
    }
}