using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Handlers;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Text.Json;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Интеграционные тесты для системы Golden Master
/// </summary>
[TestFixture]
[Category("integration")]
public class GoldenMasterIntegrationTests
{
    private Mock<ILoggingFlagsConfig> _loggingFlagsMock;
    private FakeServicesFactory _factory;
    private string _testGoldenDir;

    [SetUp]
    public void Setup()
    {
        _factory = new FakeServicesFactory();
        _loggingFlagsMock = new Mock<ILoggingFlagsConfig>();
        
        // Создаем временную директорию для тестов
        _testGoldenDir = Path.Combine(Path.GetTempPath(), $"golden_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testGoldenDir);
        
        // Меняем рабочую директорию для теста
        Environment.CurrentDirectory = _testGoldenDir;
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_testGoldenDir))
            {
                Directory.Delete(_testGoldenDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Test]
    [Category("golden-master")]
    public async Task HandleAsync_WithGoldenMasterEnabled_ShouldCreateGoldenMasterFile()
    {
        // Arrange
        _loggingFlagsMock.Setup(x => x.GoldenMasterEnabled).Returns(true);
        _loggingFlagsMock.Setup(x => x.GoldenSampleRate).Returns(1.0); // 100% sampling for test
        _loggingFlagsMock.Setup(x => x.TraceEnabled).Returns(false);

        var messageHandler = _factory.CreateMessageHandler(loggingFlags: _loggingFlagsMock.Object);
        
        var message = TestDataFactory.CreateValidMessage();
        var update = new Update
        {
            Id = 123,
            Message = message
        };

        // Act
        await messageHandler.HandleAsync(update);

        // Assert
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var expectedDir = Path.Combine("golden", today, "MessageHandler");
        
        // Note: Since MessageId is 0 by default, we look for "0.json"
        var expectedFile = Path.Combine(expectedDir, "0.json");

        Assert.That(File.Exists(expectedFile), Is.True, $"Golden Master file should exist at {expectedFile}");

        var jsonContent = await File.ReadAllTextAsync(expectedFile);
        Assert.That(jsonContent, Is.Not.Empty, "Golden Master file should not be empty");

        // Проверяем структуру JSON
        var goldenMasterData = JsonSerializer.Deserialize<JsonElement>(jsonContent);
        Assert.That(goldenMasterData.TryGetProperty("input", out _), Is.True, "Should have input property");
        Assert.That(goldenMasterData.TryGetProperty("output", out _), Is.True, "Should have output property");
    }

    [Test]
    [Category("golden-master")]
    public async Task HandleAsync_WithGoldenMasterDisabled_ShouldNotCreateGoldenMasterFile()
    {
        // Arrange
        _loggingFlagsMock.Setup(x => x.GoldenMasterEnabled).Returns(false);
        _loggingFlagsMock.Setup(x => x.TraceEnabled).Returns(false);

        var messageHandler = _factory.CreateMessageHandler(loggingFlags: _loggingFlagsMock.Object);
        
        var message = TestDataFactory.CreateValidMessage();
        var update = new Update
        {
            Id = 124,
            Message = message
        };

        // Act
        await messageHandler.HandleAsync(update);

        // Assert
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var expectedDir = Path.Combine("golden", today, "MessageHandler");
        var expectedFile = Path.Combine(expectedDir, "0.json");

        Assert.That(File.Exists(expectedFile), Is.False, "Golden Master file should not exist when disabled");
    }

    [Test]
    [Category("golden-master")]
    public async Task HandleAsync_WithTraceEnabled_ShouldLogTraceEvents()
    {
        // Arrange
        _loggingFlagsMock.Setup(x => x.GoldenMasterEnabled).Returns(false);
        _loggingFlagsMock.Setup(x => x.TraceEnabled).Returns(true);

        var loggerMock = new Mock<ILogger<MessageHandler>>();
        var messageHandler = _factory.CreateMessageHandler(logger: loggerMock.Object, loggingFlags: _loggingFlagsMock.Object);
        
        var message = TestDataFactory.CreateValidMessage();
        var update = new Update
        {
            Id = 125,
            Message = message
        };

        // Act
        await messageHandler.HandleAsync(update);

        // Assert - проверяем что трейс логи были вызваны
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("TRACE: MessageHandler->Entry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce,
            "Should log trace entry event");
    }

    [Test]
    [Category("golden-master")]
    public async Task GoldenMaster_DataShouldBeCanonical()
    {
        // Arrange
        _loggingFlagsMock.Setup(x => x.GoldenMasterEnabled).Returns(true);
        _loggingFlagsMock.Setup(x => x.GoldenSampleRate).Returns(1.0);
        _loggingFlagsMock.Setup(x => x.TraceEnabled).Returns(false);

        var messageHandler = _factory.CreateMessageHandler(loggingFlags: _loggingFlagsMock.Object);
        
        // Create a message with PII data
        var message = new Message
        {
            Date = DateTime.UtcNow,
            Text = "Test message with phone +1234567890",
            From = new User { Id = 987654321, Username = "sensitive_username" },
            Chat = new Chat { Id = -1001234567890, Type = ChatType.Group }
        };
        var update = new Update
        {
            Id = 126,
            Message = message
        };

        // Act
        await messageHandler.HandleAsync(update);

        // Assert
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var expectedFile = Path.Combine("golden", today, "MessageHandler", "0.json");
        
        var jsonContent = await File.ReadAllTextAsync(expectedFile);
        
        // Проверяем что PII замаскирована
        Assert.That(jsonContent, Does.Not.Contain("sensitive_username"), "Username should be masked");
        Assert.That(jsonContent, Does.Not.Contain("+1234567890"), "Phone should be masked");
        Assert.That(jsonContent, Does.Contain("***PHONE***"), "Should contain masked phone");
    }
}