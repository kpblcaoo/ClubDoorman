using ClubDoorman.Models.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ClubDoorman.Test.TestInfrastructure;

/// <summary>
/// Хелпер для создания mock-объектов LoggingFlags в тестах
/// </summary>
public static class LoggingFlagsTestHelper
{
    /// <summary>
    /// Создает mock IOptions&lt;LoggingFlags&gt; с настройками для тестов
    /// </summary>
    public static IOptions<LoggingFlags> CreateMockLoggingFlags(bool traceEnabled = false, bool goldenMasterEnabled = false, double sampleRate = 0.0)
    {
        var loggingFlags = new LoggingFlags
        {
            TraceEnabled = traceEnabled,
            GoldenMasterEnabled = goldenMasterEnabled,
            GoldenSampleRate = sampleRate
        };

        var mock = new Mock<IOptions<LoggingFlags>>();
        mock.Setup(x => x.Value).Returns(loggingFlags);
        return mock.Object;
    }
}