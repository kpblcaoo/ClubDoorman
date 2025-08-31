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
    /// <summary>
    /// Единый тест: проверяет что AddCommandsServices регистрирует ожидаемый набор обработчиков и сервисов.
    /// Не строим граф, чтобы не тянуть реальные зависимости модерации.
    /// </summary>
    [Test]
    public void AddCommandsServices_RegistersExpectedDescriptors()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // базовые моки (минимум чтобы не тянуть лишнее при возможном последующем расширении теста)
        services.AddSingleton(Mock.Of<ITelegramBotClientWrapper>());
        services.AddSingleton(Mock.Of<IUpdateHandler>());
        services.AddSingleton(Mock.Of<IAppConfig>());
        services.AddSingleton(Mock.Of<IModerationService>());
        services.AddSingleton(Mock.Of<IMessageService>());
        services.AddSingleton(Mock.Of<ISpamHamClassifier>());
        services.AddSingleton(Mock.Of<IBotPermissionsService>());
        services.AddSingleton(Mock.Of<IBadMessageManager>());
        services.AddSingleton(Mock.Of<IAiChecks>());
        services.AddSingleton(Mock.Of<IStatisticsService>());
        services.AddSingleton(Mock.Of<IUserFlowLogger>());
        services.AddSingleton(Mock.Of<IChatLinkFormatter>());
        services.AddSingleton(Mock.Of<IViolationTracker>());
        services.AddSingleton(Mock.Of<IUserBanService>());
        services.AddSingleton(Mock.Of<IChannelModerationService>());
        services.AddSingleton(Mock.Of<ILogChatService>());
        services.AddSingleton<ClubDoorman.Features.Moderation.IModerationFacade>(_ => Mock.Of<ClubDoorman.Features.Moderation.IModerationFacade>()); // защитный мок

        // act
        services.AddCommandsServices();

        // ожидаемые обработчики
        var expectedHandlerTypes = new[]
        {
            typeof(SpamCommandHandler),
            typeof(HamCommandHandler),
            typeof(CheckCommandHandler),
            typeof(StatsCommandHandler),
            typeof(SayCommandHandler),
            typeof(SuspiciousCommandHandler),
            typeof(StartCommandHandler)
        };

        foreach (var handlerType in expectedHandlerTypes)
        {
            var registered = services.Any(d => d.ServiceType == typeof(ICommandHandler) && d.ImplementationType == handlerType);
            Assert.That(registered, Is.True, $"Handler {handlerType.Name} must be registered as ICommandHandler");
        }

        // отдельные обратные совместимости
        Assert.That(services.Any(d => d.ServiceType == typeof(IStartCommandHandler) && d.ImplementationType == typeof(StartCommandHandler)), Is.True);
        Assert.That(services.Any(d => d.ServiceType == typeof(ISuspiciousCommandHandler) && d.ImplementationType == typeof(SuspiciousCommandHandler)), Is.True);
        Assert.That(services.Any(d => d.ServiceType == typeof(ICommandProcessingService) && d.ImplementationType == typeof(CommandProcessingService)), Is.True);
        Assert.That(services.Any(d => d.ServiceType == typeof(IAdminOpsFacade) && d.ImplementationType == typeof(AdminOpsFacade)), Is.True);
    }
}