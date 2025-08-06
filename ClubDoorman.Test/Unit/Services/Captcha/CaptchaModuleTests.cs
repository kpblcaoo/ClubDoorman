using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Core.Configuration;

namespace ClubDoorman.Test.Unit.Services.Captcha;

/// <summary>
/// Тесты для CaptchaModule
/// <tags>unit, captcha-module, di-registration</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("captcha-module")]
public class CaptchaModuleTests
{
    private IServiceCollection _services = null!;

    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection();
        
        // Добавляем необходимые зависимости для тестирования
        _services.AddSingleton(Mock.Of<ITelegramBotClientWrapper>());
        _services.AddSingleton(Mock.Of<IMessageService>());
        _services.AddSingleton(Mock.Of<IAppConfig>());
        
        // Добавляем логгеры
        _services.AddLogging();
    }

    /// <summary>
    /// POC: Проверка регистрации ICaptchaService
    /// <tags>poc, captcha-service, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCaptchaServices_ShouldRegisterICaptchaService()
    {
        // Act
        _services.AddCaptchaServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var captchaService = serviceProvider.GetService<ICaptchaService>();
        Assert.That(captchaService, Is.Not.Null);
    }

    /// <summary>
    /// POC: Проверка регистрации CaptchaService через интерфейс
    /// <tags>poc, captcha-service, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCaptchaServices_ShouldRegisterCaptchaServiceAsICaptchaService()
    {
        // Act
        _services.AddCaptchaServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var captchaService = serviceProvider.GetService<ICaptchaService>();
        Assert.That(captchaService, Is.InstanceOf<CaptchaService>());
    }



    /// <summary>
    /// POC: Проверка, что метод возвращает IServiceCollection
    /// <tags>poc, captcha-service, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCaptchaServices_ShouldReturnServiceCollection()
    {
        // Act
        var result = _services.AddCaptchaServices();

        // Assert
        Assert.That(result, Is.SameAs(_services));
    }
} 