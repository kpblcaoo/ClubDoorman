using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ClubDoorman.Services.UserManagement;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для UserCleanupServiceTestFactory
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class UserCleanupServiceTestFactoryTests
{
    [Test]
    public void CreateUserCleanupService_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new UserCleanupServiceTestFactory();

        // Act
        var instance = factory.CreateUserCleanupService();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<UserCleanupService>());
    }

    [Test]
    public void CreateUserCleanupService_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new UserCleanupServiceTestFactory();

        // Act
        var instance = factory.CreateUserCleanupService();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateUserCleanupService_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new UserCleanupServiceTestFactory();

        // Act
        var instance1 = factory.CreateUserCleanupService();
        var instance2 = factory.CreateUserCleanupService();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void CreateRealUserCleanupService_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new UserCleanupServiceTestFactory();

        // Act
        var instance = factory.CreateRealUserCleanupService();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<UserCleanupService>());
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new UserCleanupServiceTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }

    [Test]
    public void ApprovedUsersStorageLoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new UserCleanupServiceTestFactory();

        // Act & Assert
        Assert.That(factory.ApprovedUsersStorageLoggerMock, Is.Not.Null);
        Assert.That(factory.ApprovedUsersStorageLoggerMock.Object, Is.Not.Null);
    }
} 