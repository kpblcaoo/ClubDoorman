using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.UserJoin;
using ClubDoorman.Features.UserJoin;

namespace ClubDoorman.Test.Unit.Services.UserManagement;

[TestFixture]
public class UserManagementModuleTests
{
    [Test]
    public void AddUserManagementServices_ShouldRegisterIUserManager()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddUserManagementServices();
        
        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUserManager));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(UserManager)));
    }
    
    [Test]
    public void AddUserManagementServices_ShouldRegisterApprovedUsersStorage()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddUserManagementServices();
        
        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ApprovedUsersStorage));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(ApprovedUsersStorage)));
    }
    
    [Test]
    public void AddUserManagementServices_ShouldRegisterIUserCleanupService()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddUserManagementServices();
        
        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUserCleanupService));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(UserCleanupService)));
    }
    
    [Test]
    public void AddUserManagementServices_ShouldRegisterUserIndex()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddUserManagementServices();
        
        // Assert
    var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUserIndex));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(UserIndex)));
    }
    
    [Test]
    public void AddUserManagementServices_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        var result = services.AddUserManagementServices();
        
        // Assert
        Assert.That(result, Is.SameAs(services));
    }
} 