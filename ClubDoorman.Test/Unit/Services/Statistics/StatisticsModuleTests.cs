using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ClubDoorman.Services.Statistics;

namespace ClubDoorman.Test.Unit.Services.Statistics;

[TestFixture]
public class StatisticsModuleTests
{
    [Test]
    public void AddStatisticsServices_ShouldRegisterIStatisticsService()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddStatisticsServices();
        
        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IStatisticsService));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(StatisticsService)));
    }
    
    [Test]
    public void AddStatisticsServices_ShouldRegisterGlobalStatsManager()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddStatisticsServices();
        
        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(GlobalStatsManager));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(GlobalStatsManager)));
    }
    
    [Test]
    public void AddStatisticsServices_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        var result = services.AddStatisticsServices();
        
        // Assert
        Assert.That(result, Is.SameAs(services));
    }
} 