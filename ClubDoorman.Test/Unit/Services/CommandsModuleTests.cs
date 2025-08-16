using ClubDoorman.Services.Moderation; // legacy interfaces (IModerationService) still used некоторыми командами
// NOTE: Мы сознательно НЕ подтягиваем ModerationFeature здесь, чтобы unit-тест оставался изолированным и проверял только регистрацию команд.
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Features.Moderation;
using Moq;

namespace ClubDoorman.Test.Unit.Services.Commands;

/// <summary>
/// Тесты для CommandsModule
/// <tags>unit, commands-module, di-registration</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("commands-module")]
public class CommandsModuleTests
{
    private IServiceCollection _services = null!;

    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection();
        
        // Добавляем необходимые зависимости для тестирования
        _services.AddSingleton(Mock.Of<ITelegramBotClientWrapper>());
        _services.AddSingleton(Mock.Of<IUpdateHandler>());
        _services.AddSingleton(Mock.Of<IAppConfig>());
    _services.AddSingleton(Mock.Of<IModerationService>()); // оставить пока: часть хендлеров ещё зависит от старого сервиса
        _services.AddSingleton(Mock.Of<IMessageService>());
        _services.AddSingleton(Mock.Of<ISpamHamClassifier>());
        _services.AddSingleton(Mock.Of<IBotPermissionsService>());
        _services.AddSingleton(Mock.Of<IBadMessageManager>());
        _services.AddSingleton(Mock.Of<IAiChecks>());
        _services.AddSingleton(Mock.Of<IStatisticsService>());
        _services.AddSingleton(Mock.Of<IUserFlowLogger>());
        _services.AddSingleton(Mock.Of<IChatLinkFormatter>());
        _services.AddSingleton(Mock.Of<IViolationTracker>());
        _services.AddSingleton(Mock.Of<IUserBanService>());
    _services.AddSingleton(Mock.Of<IChannelModerationService>());
    _services.AddSingleton(Mock.Of<ILogChatService>());
    // Не регистрируем ModerationFacade / ModerationFeature: этот тест не обязан материализовать SuspiciousCommandHandler.
    // Мы проверяем сам факт регистрации дескрипторов, а не успешное создание экземпляров, чтобы избежать каскада зависимостей.
    _services.AddSingleton<ClubDoorman.Features.Moderation.IModerationFacade>(_ => Mock.Of<ClubDoorman.Features.Moderation.IModerationFacade>()); // минимальный мок для зависимостей SuspiciousCommandHandler
        
        // Добавляем логгеры
        _services.AddLogging();
    }

    /// <summary>
    /// POC: Проверка регистрации ICommandProcessingService
    /// <tags>poc, command-processing-service, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCommandsServices_ShouldRegisterICommandProcessingService()
    {
    // Act
    _services.AddCommandsServices();

    // Assert (проверяем саму регистрацию, без инстанцирования графа)
    var registered = _services.Any(d => d.ServiceType == typeof(ICommandProcessingService));
    Assert.That(registered, Is.True, "ICommandProcessingService descriptor should be registered");
    }

    /// <summary>
    /// POC: Проверка регистрации ICommandHandler для StartCommandHandler
    /// <tags>poc, start-command-handler, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCommandsServices_ShouldRegisterStartCommandHandlerAsICommandHandler()
    {
    // Act
    _services.AddCommandsServices();

    // Assert: проверяем что дескриптор StartCommandHandler зарегистрирован как ICommandHandler
    var hasStart = _services.Any(d => d.ServiceType == typeof(ICommandHandler) && d.ImplementationType == typeof(StartCommandHandler));
    Assert.That(hasStart, Is.True, "StartCommandHandler must be registered as ICommandHandler");
    }

    /// <summary>
    /// POC: Проверка регистрации ICommandHandler для SuspiciousCommandHandler
    /// <tags>poc, suspicious-command-handler, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCommandsServices_ShouldRegisterSuspiciousCommandHandlerAsICommandHandler()
    {
    // Act
    _services.AddCommandsServices();

    // Assert: проверяем что SuspiciousCommandHandler зарегистрирован (не создаём экземпляр -> не нужен IModerationFacade)
    var hasSuspicious = _services.Any(d => d.ServiceType == typeof(ICommandHandler) && d.ImplementationType == typeof(SuspiciousCommandHandler));
    Assert.That(hasSuspicious, Is.True, "SuspiciousCommandHandler must be registered as ICommandHandler");
    }

    /// <summary>
    /// POC: Проверка, что метод возвращает IServiceCollection
    /// <tags>poc, commands-service, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCommandsServices_ShouldReturnServiceCollection()
    {
        // Act
        var result = _services.AddCommandsServices();

        // Assert
        Assert.That(result, Is.SameAs(_services));
    }
} 