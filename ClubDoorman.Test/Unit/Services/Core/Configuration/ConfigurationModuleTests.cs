using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ClubDoorman.Services.Core.Configuration;

namespace ClubDoorman.Test.Unit.Services.Core.Configuration;

[TestFixture]
public class ConfigurationModuleTests
{
    [Test]
    public void AddConfigurationServices_ShouldRegisterIAppConfig()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddConfigurationServices();
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var appConfig = serviceProvider.GetService<IAppConfig>();
        Assert.That(appConfig, Is.Not.Null);
        Assert.That(appConfig, Is.InstanceOf<AppConfig>());
    }
    
    [Test]
    public void AddConfigurationServices_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        var result = services.AddConfigurationServices();
        
        // Assert
        Assert.That(result, Is.SameAs(services));
    }
} 