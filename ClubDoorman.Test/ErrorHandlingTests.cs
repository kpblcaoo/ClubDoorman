using ClubDoorman.Infrastructure.ErrorHandling;
using ClubDoorman.Infrastructure.ErrorHandling.ErrorHandlingStrategies;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.Test;

/// <summary>
/// Тесты для централизованной системы обработки ошибок
/// </summary>
public class ErrorHandlingTests : TestBase
{
    [Test]
    public void ErrorContext_Constructor_WithOperation_SetsProperties()
    {
        // Arrange & Act
        var context = new ErrorContext("TestOperation", "Test description", ErrorSeverity.High);

        // Assert
        Assert.That(context.Operation, Is.EqualTo("TestOperation"));
        Assert.That(context.Description, Is.EqualTo("Test description"));
        Assert.That(context.Severity, Is.EqualTo(ErrorSeverity.High));
    }

    [Test]
    public void ErrorContext_WithData_AddsAdditionalData()
    {
        // Arrange
        var context = new ErrorContext("TestOperation");

        // Act
        context.WithData("key1", "value1").WithData("key2", 42);

        // Assert
        Assert.That(context.AdditionalData["key1"], Is.EqualTo("value1"));
        Assert.That(context.AdditionalData["key2"], Is.EqualTo(42));
    }

    [Test]
    public void ErrorContext_WithSeverity_SetsSeverity()
    {
        // Arrange
        var context = new ErrorContext("TestOperation");

        // Act
        context.WithSeverity(ErrorSeverity.Critical);

        // Assert
        Assert.That(context.Severity, Is.EqualTo(ErrorSeverity.Critical));
    }

    [Test]
    public void LoggingStrategy_CanHandle_ReturnsTrue()
    {
        // Arrange
        var logger = new Mock<ILogger<LoggingStrategy>>().Object;
        var strategy = new LoggingStrategy(logger);
        var exception = new InvalidOperationException("Test exception");
        var context = new ErrorContext("TestOperation");

        // Act
        var result = strategy.CanHandle(exception, context);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void NotificationStrategy_CanHandle_HighSeverity_ReturnsTrue()
    {
        // Arrange
        var messageService = new Mock<IMessageService>().Object;
        var logger = new Mock<ILogger<NotificationStrategy>>().Object;
        var strategy = new NotificationStrategy(messageService, logger);
        var exception = new InvalidOperationException("Test exception");
        var context = new ErrorContext("TestOperation", severity: ErrorSeverity.High);

        // Act
        var result = strategy.CanHandle(exception, context);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void NotificationStrategy_CanHandle_LowSeverity_ReturnsFalse()
    {
        // Arrange
        var messageService = new Mock<IMessageService>().Object;
        var logger = new Mock<ILogger<NotificationStrategy>>().Object;
        var strategy = new NotificationStrategy(messageService, logger);
        var exception = new InvalidOperationException("Test exception");
        var context = new ErrorContext("TestOperation", severity: ErrorSeverity.Low);

        // Act
        var result = strategy.CanHandle(exception, context);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void RetryStrategy_IsRetryableException_HttpRequestException_ReturnsTrue()
    {
        // Arrange
        var logger = new Mock<ILogger<RetryStrategy>>().Object;
        var strategy = new RetryStrategy(logger);
        var exception = new HttpRequestException("Network error");
        var context = new ErrorContext("TestOperation");

        // Act
        var result = strategy.CanHandle(exception, context);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void RetryStrategy_IsRetryableException_OperationCanceledException_ReturnsFalse()
    {
        // Arrange
        var logger = new Mock<ILogger<RetryStrategy>>().Object;
        var strategy = new RetryStrategy(logger);
        var exception = new OperationCanceledException("Operation canceled");
        var context = new ErrorContext("TestOperation");

        // Act
        var result = strategy.CanHandle(exception, context);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ErrorHandlingResult_Success_CreatesSuccessResult()
    {
        // Act
        var result = ErrorHandlingResult.Success(shouldContinue: false);

        // Assert
        Assert.That(result.IsHandled, Is.True);
        Assert.That(result.ShouldContinue, Is.False);
    }

    [Test]
    public void ErrorHandlingResult_Failure_CreatesFailureResult()
    {
        // Act
        var result = ErrorHandlingResult.Failure(shouldContinue: true);

        // Assert
        Assert.That(result.IsHandled, Is.False);
        Assert.That(result.ShouldContinue, Is.True);
    }

    [Test]
    public void ErrorHandlingResult_WithData_AddsAdditionalData()
    {
        // Arrange
        var result = ErrorHandlingResult.Success();

        // Act
        result.WithData("key1", "value1").WithData("key2", 42);

        // Assert
        Assert.That(result.AdditionalData["key1"], Is.EqualTo("value1"));
        Assert.That(result.AdditionalData["key2"], Is.EqualTo(42));
    }
} 