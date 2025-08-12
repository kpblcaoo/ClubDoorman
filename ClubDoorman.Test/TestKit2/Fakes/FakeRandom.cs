using System;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public interface IRandom
{
    int Next(int minValue, int maxValue);
    double NextDouble();
}

public sealed class SeededRandom : IRandom
{
    private readonly Random _random;
    
    public SeededRandom(int seed = 42) => _random = new Random(seed);
    
    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
    public double NextDouble() => _random.NextDouble();
}
