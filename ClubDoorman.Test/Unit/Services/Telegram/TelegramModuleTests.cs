using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Test.Unit.Services.Telegram;

[TestFixture]
public class TelegramModuleTests
{
    [Test]
    public void AddTelegramServices_ShouldRegisterITelegramBotClientWrapper()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddTelegramServices();
        
        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITelegramBotClientWrapper));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(TelegramBotClientWrapper)));
    }
    
    [Test]
    public void AddTelegramServices_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        var result = services.AddTelegramServices();
        
        // Assert
        Assert.That(result, Is.SameAs(services));
    }
} 