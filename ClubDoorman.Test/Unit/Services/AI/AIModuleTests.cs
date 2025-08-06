using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ClubDoorman.Services.AI;

namespace ClubDoorman.Test.Unit.Services.AI;

[TestFixture]
public class AIModuleTests
{
    [Test]
    public void AddAIServices_ShouldRegisterIAiChecks()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddAIServices();
        
        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAiChecks));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(AiChecks)));
    }
    
    [Test]
    public void AddAIServices_ShouldRegisterISpamHamClassifier()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddAIServices();
        
        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISpamHamClassifier));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(SpamHamClassifier)));
    }
    
    [Test]
    public void AddAIServices_ShouldRegisterIMimicryClassifier()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddAIServices();
        
        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMimicryClassifier));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(MimicryClassifier)));
    }
    
    [Test]
    public void AddAIServices_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        var result = services.AddAIServices();
        
        // Assert
        Assert.That(result, Is.SameAs(services));
    }
} 