using AutoFixture;
using AutoFixture.AutoMoq;

using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;

using ClubDoorman.Services;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Test.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Commands;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test.TestKit.Infra;

/// <summary>
/// Расширение TestKit с AutoFixture для автоматического создания тестовых объектов
/// <tags>autofixture, auto-generation, dependencies, test-infrastructure</tags>
/// </summary>
public static class TestKitAutoFixture
{
    private static readonly IFixture _fixture = CreateFixture();

    /// <summary>
    /// Создает настроенный AutoFixture с кастомизациями для Telegram-типов
    /// <tags>autofixture, customization, telegram, test-infrastructure</tags>
    /// </summary>
    public static IFixture CreateFixture()
    {
        var fixture = new Fixture()
            .Customize(new AutoMoqCustomization { ConfigureMembers = true });

        // Кастомизации для Telegram-типов
        fixture.Customize<Telegram.Bot.Types.User>(composer => composer
            .FromFactory(() => TestKitBogus.CreateRealisticUser())
            .OmitAutoProperties());

        fixture.Customize<Telegram.Bot.Types.Chat>(composer => composer
            .FromFactory(() => TestKitBogus.CreateRealisticGroup())
            .OmitAutoProperties());

        fixture.Customize<Telegram.Bot.Types.Message>(composer => composer
            .FromFactory(() => TestKitBogus.CreateRealisticMessage())
            .OmitAutoProperties());

        // Кастомизации для наших сервисов
        fixture.Customize<ILogger<MessageHandler>>(composer => composer
            .FromFactory(() => NullLogger<MessageHandler>.Instance));

        fixture.Customize<ILogger<ModerationService>>(composer => composer
            .FromFactory(() => NullLogger<ModerationService>.Instance));

        fixture.Customize<ILogger<CaptchaService>>(composer => composer
            .FromFactory(() => NullLogger<CaptchaService>.Instance));

        fixture.Customize<ILogger<IAiChecks>>(composer => composer
            .FromFactory(() => NullLogger<IAiChecks>.Instance));

        // Кастомизации для внешних зависимостей
        fixture.Customize<ITelegramBotClientWrapper>(composer => composer
            .FromFactory(() => TK.CreateMockBotClientWrapper().Object));

        fixture.Customize<IModerationService>(composer => composer
            .FromFactory(() => TK.CreateMockModerationService().Object));

        fixture.Customize<ICaptchaService>(composer => composer
            .FromFactory(() => TK.CreateMockCaptchaService().Object));

        fixture.Customize<IUserManager>(composer => composer
            .FromFactory(() => TK.CreateMockUserManager().Object));

        fixture.Customize<IChannelModerationService>(composer => composer
            .FromFactory(() => TK.CreateMock<IChannelModerationService>().Object));

        // Отключаем AutoProperties для MessageHandler чтобы избежать проблем с DI
        fixture.Customize<MessageHandler>(composer => composer
            .FromFactory(() =>
            {
                var bot = TK.CreateMockBotClientWrapper().Object;
                var moderationService = TK.CreateMockModerationService().Object;
                var captchaService = TK.CreateMockCaptchaService().Object;
                var userManager = TK.CreateMockUserManager().Object;
                var classifier = TK.CreateMockSpamHamClassifier().Object;
                var badMessageManager = new Mock<IBadMessageManager>().Object;
                var aiChecks = TK.CreateMockAiChecks().Object;
                var globalStatsManager = new GlobalStatsManager();
                var statisticsService = TK.CreateMockStatisticsService().Object;
                var userFlowLogger = new Mock<IUserFlowLogger>().Object;
                var messageService = TK.CreateMockMessageService().Object;
                var chatLinkFormatter = new Mock<IChatLinkFormatter>().Object;
                var botPermissionsService = TK.CreateMockBotPermissionsService().Object;
                var appConfig = TK.CreateMockAppConfig().Object;
                var violationTracker = new ViolationTracker(new Mock<ILogger<ViolationTracker>>().Object, appConfig);
                var logger = new Mock<ILogger<MessageHandler>>().Object;
                var userBanService = TK.CreateMockUserBanService().Object;
                var channelModerationService = TK.CreateMock<IChannelModerationService>().Object;
                var startCommandHandler = new Mock<StartCommandHandler>(
                    Mock.Of<ITelegramBotClientWrapper>(),
                    Mock.Of<ILogger<StartCommandHandler>>(),
                    Mock.Of<IMessageService>(),
                    Mock.Of<IAppConfig>()).Object;
                var suspiciousCommandHandler = new Mock<SuspiciousCommandHandler>(
                    Mock.Of<ITelegramBotClientWrapper>(),
                    Mock.Of<IModerationService>(),
                    Mock.Of<IMessageService>(),
                    Mock.Of<ILogger<SuspiciousCommandHandler>>(),
                    Mock.Of<IAppConfig>()).Object;
                var logChatService = TK.CreateMock<ILogChatService>().Object;
                var commandRouter = TK.CreateMock<ICommandRouter>().Object;
                var aiCascadeService = TK.CreateMock<IAiCascadeService>().Object;
                var notificationService = TK.CreateMock<INotificationService>().Object;
                var forwardingService = TK.CreateMock<ClubDoorman.Services.Notifications.IForwardingService>().Object;
                var buttonsService = TK.CreateMock<ClubDoorman.Services.Notifications.IButtonsService>().Object;
                var joinedUserFlags = TK.CreateMock<IJoinedUserFlags>().Object;
                var userIndex = TK.CreateMock<IUserIndex>().Object;

                return new MessageHandler(
                    bot, moderationService, captchaService, userManager, classifier,
                    badMessageManager, aiChecks, globalStatsManager, statisticsService,
                    userFlowLogger, messageService, chatLinkFormatter,
                    botPermissionsService, appConfig, violationTracker, logger, userBanService,
                    channelModerationService, startCommandHandler, suspiciousCommandHandler, commandRouter, logChatService,
                    joinedUserFlags, userIndex, aiCascadeService, notificationService, forwardingService, buttonsService);
            })
            .OmitAutoProperties());

        return fixture;
    }

    /// <summary>
    /// Получает глобальный экземпляр AutoFixture
    /// <tags>autofixture, global, singleton, test-infrastructure</tags>
    /// </summary>
    public static IFixture GetFixture() => _fixture;

    /// <summary>
    /// Создает объект указанного типа
    /// <tags>autofixture, create, generic, test-infrastructure</tags>
    /// </summary>
    public static T Create<T>() => _fixture.Create<T>();

    /// <summary>
    /// Создает коллекцию объектов указанного типа
    /// <tags>autofixture, create-many, collection, test-infrastructure</tags>
    /// </summary>
    public static IEnumerable<T> CreateMany<T>(int count = 3) => _fixture.CreateMany<T>(count);

    /// <summary>
    /// Заполняет свойства существующего объекта
    /// <tags>autofixture, populate, fill-properties, test-infrastructure</tags>
    /// </summary>
    public static T Populate<T>(T item) where T : class
    {
        _fixture.Build<T>().FromSeed(s => item).Create();
        return item;
    }

    #region Специализированные методы создания

    /// <summary>
    /// Создает MessageHandler с автозависимостями
    /// <tags>autofixture, message-handler, dependencies, test-infrastructure</tags>
    /// </summary>
    public static MessageHandler CreateMessageHandler()
    {
        return _fixture.Create<MessageHandler>();
    }

    /// <summary>
    /// Создает ModerationService с автозависимостями
    /// <tags>autofixture, moderation-service, dependencies, test-infrastructure</tags>
    /// </summary>
    public static ModerationService CreateModerationService()
    {
        return _fixture.Create<ModerationService>();
    }

    /// <summary>
    /// Создает список реалистичных пользователей
    /// <tags>autofixture, users, realistic, test-infrastructure</tags>
    /// </summary>
    public static List<Telegram.Bot.Types.User> CreateRealisticUsers(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(_ => TestKitBogus.CreateRealisticUser())
            .ToList();
    }

    /// <summary>
    /// Создает список реалистичных сообщений
    /// <tags>autofixture, messages, realistic, test-infrastructure</tags>
    /// </summary>
    public static List<Telegram.Bot.Types.Message> CreateRealisticMessages(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(_ => TestKitBogus.CreateRealisticMessage())
            .ToList();
    }

    /// <summary>
    /// Создает тестовый сценарий с пользователем, чатом и сообщением
    /// <tags>autofixture, scenario, user-chat-message, test-infrastructure</tags>
    /// </summary>
    public static (Telegram.Bot.Types.User User, Telegram.Bot.Types.Chat Chat, Telegram.Bot.Types.Message Message) CreateTestScenario()
    {
        var user = TestKitBogus.CreateRealisticUser();
        var chat = TestKitBogus.CreateRealisticGroup();
        var message = TestKitBogus.CreateRealisticMessage();
        
        // Связываем объекты
        message.From = user;
        message.Chat = chat;
        
        return (user, chat, message);
    }

    #endregion

    #region Методы для работы с коллекциями

    /// <summary>
    /// Создает произвольное количество объектов (от 1 до 5)
    /// <tags>autofixture, random-count, flexible, test-infrastructure</tags>
    /// </summary>
    public static IEnumerable<T> CreateSome<T>() 
    {
        var count = new Random().Next(1, 6); // 1-5 объектов
        return CreateMany<T>(count);
    }

    /// <summary>
    /// Создает объект с переопределенными свойствами
    /// <tags>autofixture, override, customization, test-infrastructure</tags>
    /// </summary>
    public static T CreateWith<T>(Action<T> customization) where T : class
    {
        var obj = Create<T>();
        customization(obj);
        return obj;
    }

    #endregion

    #region Утилиты для тестирования

    /// <summary>
    /// Замораживает фиксированные значения для воспроизводимых тестов
    /// <tags>autofixture, freeze, reproducible, test-infrastructure</tags>
    /// </summary>
    public static T Freeze<T>()
    {
        return _fixture.Freeze<T>();
    }

    /// <summary>
    /// Регистрирует кастомную фабрику для типа
    /// <tags>autofixture, customize, factory, test-infrastructure</tags>
    /// </summary>
    public static void CustomizeWith<T>(Func<T> factory)
    {
        _fixture.Customize<T>(composer => composer.FromFactory(factory));
    }

    /// <summary>
    /// Инжектирует конкретный экземпляр для типа
    /// <tags>autofixture, inject, singleton, test-infrastructure</tags>
    /// </summary>
    public static void Inject<T>(T instance)
    {
        _fixture.Inject(instance);
    }

    #endregion
    
    #region Backward Compatibility Methods
    
    /// <summary>
    /// Создает CaptchaService с автозависимостями
    /// <tags>autofixture, captcha-service, dependencies, test-infrastructure</tags>
    /// </summary>
    public static ICaptchaService CreateCaptchaService() => _fixture.Create<ICaptchaService>();
    
    /// <summary>
    /// Создает UserManager с автозависимостями
    /// <tags>autofixture, user-manager, dependencies, test-infrastructure</tags>
    /// </summary>
    public static IUserManager CreateUserManager() => _fixture.Create<IUserManager>();
    
    /// <summary>
    /// Создает Update объект
    /// <tags>autofixture, update, telegram, test-infrastructure</tags>
    /// </summary>
    public static Telegram.Bot.Types.Update CreateUpdate() => _fixture.Create<Telegram.Bot.Types.Update>();
    
    /// <summary>
    /// Создает MessageUpdate
    /// <tags>autofixture, message-update, telegram, test-infrastructure</tags>
    /// </summary>
    public static Telegram.Bot.Types.Update CreateMessageUpdate()
    {
        var update = _fixture.Create<Telegram.Bot.Types.Update>();
        update.Message = TestKitBogus.CreateRealisticMessage();
        return update;
    }
    
    /// <summary>
    /// Создает CallbackQueryUpdate
    /// <tags>autofixture, callback-query-update, telegram, test-infrastructure</tags>
    /// </summary>
    public static Telegram.Bot.Types.Update CreateCallbackQueryUpdate()
    {
        var update = _fixture.Create<Telegram.Bot.Types.Update>();
        update.CallbackQuery = _fixture.Create<Telegram.Bot.Types.CallbackQuery>();
        return update;
    }
    
    /// <summary>
    /// Создает много сообщений
    /// <tags>autofixture, many-messages, collection, test-infrastructure</tags>
    /// </summary>
    public static List<Telegram.Bot.Types.Message> CreateManyMessages(int count = 3) => 
        Enumerable.Range(0, count).Select(_ => TestKitBogus.CreateRealisticMessage()).ToList();
    
    /// <summary>
    /// Создает много пользователей
    /// <tags>autofixture, many-users, collection, test-infrastructure</tags>
    /// </summary>
    public static List<Telegram.Bot.Types.User> CreateManyUsers(int count = 3) => 
        Enumerable.Range(0, count).Select(_ => TestKitBogus.CreateRealisticUser()).ToList();
    
    /// <summary>
    /// Создает много спам-сообщений
    /// <tags>autofixture, many-spam-messages, collection, test-infrastructure</tags>
    /// </summary>
    public static List<Telegram.Bot.Types.Message> CreateManySpamMessages(int count = 3) => 
        Enumerable.Range(0, count).Select(_ => TestKitBogus.CreateSpamMessage()).ToList();
    
    /// <summary>
    /// Создает объект с фикстурой для кастомизации
    /// <tags>autofixture, customization, fixture, test-infrastructure</tags>
    /// </summary>
    public static (T sut, IFixture fixture) CreateWithFixture<T>()
    {
        var fixture = CreateFixture(); // Используем нашу кастомизированную фикстуру
        var sut = fixture.Create<T>();
        return (sut, fixture);
    }

    #endregion
}