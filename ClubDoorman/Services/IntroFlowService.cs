using System.Diagnostics;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.Services.BanSystem;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Models.Requests;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для обработки приветствия новых участников
/// </summary>
public class IntroFlowService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<IntroFlowService> _logger;
    private readonly ICaptchaService _captchaService;
    private readonly IUserManager _userManager;
    private readonly IAiChecks _aiChecks;
    private readonly IStatisticsService _statisticsService;
    private readonly GlobalStatsManager _globalStatsManager;
    private readonly IModerationService _moderationService;
    private readonly IMessageService _messageService;
    private readonly IUserBanService _userBanService;
    private readonly IAppConfig _appConfig;

    public IntroFlowService(
        ITelegramBotClientWrapper bot,
        ILogger<IntroFlowService> logger,
        ICaptchaService captchaService,
        IUserManager userManager,
        IAiChecks aiChecks,
        IStatisticsService statisticsService,
        GlobalStatsManager globalStatsManager,
        IModerationService moderationService,
        IMessageService messageService,
        IUserBanService userBanService,
        IAppConfig appConfig)
    {
        _bot = bot;
        _logger = logger;
        _captchaService = captchaService;
        _userManager = userManager;
        _aiChecks = aiChecks;
        _statisticsService = statisticsService;
        _globalStatsManager = globalStatsManager;
        _moderationService = moderationService;
        _messageService = messageService;
        _userBanService = userBanService;
        _appConfig = appConfig;
    }

    public async Task ProcessNewUserAsync(Message? userJoinMessage, User user, Chat? chat = default, CancellationToken cancellationToken = default)
    {
        chat = userJoinMessage?.Chat ?? chat;
        Debug.Assert(chat != null);
        
        // Проверка whitelist - если активен, работаем только в разрешённых чатах
        if (!_appConfig.IsChatAllowed(chat.Id))
        {
            _logger.LogDebug("Чат {ChatId} ({ChatTitle}) не в whitelist - игнорируем IntroFlow", chat.Id, chat.Title);
            return;
        }
        
        // Проверяем длину имени
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        
        // Проверяем длину имени для обоих случаев
        if (fullName.Length > 40)
        {
            var isPermanent = fullName.Length > 75;
            await BanUserForLongNameAsync(
                userJoinMessage,
                user,
                fullName,
                isPermanent ? null : TimeSpan.FromMinutes(10),
                isPermanent ? "🚫 Перманентный бан" : "Автобан на 10 минут",
                isPermanent ? "экстремально" : "подозрительно",
                chat
            );
            return;
        }

        _logger.LogDebug("Intro flow {@User}", user);
        
        // Проверяем, является ли пользователь участником клуба
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (clubUser != null)
        {
            _logger.LogDebug("User is {Name} from club", clubUser);
            return;
        }

        var chatId = chat.Id;

        if (await BanIfBlacklisted(user, chat, userJoinMessage))
            return;

        // AI анализ профиля убран - теперь выполняется при первом сообщении в MessageHandler

        var key = _captchaService.GenerateKey(chatId, user.Id);
        if (_captchaService.GetCaptchaInfo(key) != null)
        {
            _logger.LogDebug("This user is already awaiting captcha challenge");
            return;
        }

        // Создаем капчу через сервис
        var captchaRequest = new CreateCaptchaRequest(chat, user, userJoinMessage);
        var captchaInfo = await _captchaService.CreateCaptchaAsync(captchaRequest);
        
        // Если капча отключена для этой группы, отправляем приветствие сразу
        if (captchaInfo == null)
        {
            _logger.LogInformation("[NO_CAPTCHA] Капча отключена для чата {ChatId} - отправляем приветствие сразу после проверок", chat.Id);
            var welcomeRequest = new SendWelcomeMessageRequest(user, chat, "приветствие без капчи", cancellationToken);
            await _messageService.SendWelcomeMessageAsync(welcomeRequest);
        }
        else
        {
            _globalStatsManager.IncCaptcha(chatId, chat.Title ?? "");
        }
    }

    private async Task BanUserForLongNameAsync(
        Message? userJoinMessage,
        User user,
        string fullName,
        TimeSpan? banDuration,
        string banType,
        string nameDescription,
        Chat? chat = default)
    {
        try
        {
            chat = userJoinMessage?.Chat ?? chat;
            Debug.Assert(chat != null);
            
            // Определяем тип бана на основе длительности
            var banTypeEnum = banDuration.HasValue ? BanTypeEnum.LongName : BanTypeEnum.LongName;
            var reason = $"{nameDescription} длинное имя пользователя ({fullName.Length} символов): {fullName}";
            
            // Используем UserBanService для централизованного бана
            await _userBanService.BanUserAsync(chat, user, banTypeEnum, reason, userJoinMessage, CancellationToken.None);
            
            // Логируем для статистики
            _statisticsService.IncrementLongNameBan(chat.Id);

            // Уведомляем админов
            await _messageService.SendAdminNotificationAsync(
                AdminNotificationType.AutoBan,
                new AutoBanNotificationData(user, chat, banType, reason)
            );
            _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban user with long username");
        }
    }

    private async Task<bool> BanIfBlacklisted(User user, Chat chat, Message? userJoinMessage = null)
    {
        if (!await _userManager.InBanlist(user.Id))
            return false;

        try
        {
            // Используем UserBanService для централизованного бана из блэклиста
            if (userJoinMessage != null)
            {
                await _userBanService.BanBlacklistedUserAsync(userJoinMessage, user, CancellationToken.None);
            }
            else
            {
                // Если нет сообщения о входе, используем BanUserAsync с BanTypeEnum.Blacklist
                await _userBanService.BanUserAsync(chat, user, BanTypeEnum.Blacklist, "Пользователь в блэклисте", null, CancellationToken.None);
            }
            
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban");
            await _messageService.SendAdminNotificationAsync(
                AdminNotificationType.Warning,
                new SimpleNotificationData(new User { Id = 0, FirstName = "System" }, chat, $"Не могу забанить юзера из блеклиста в чате {chat.Title}. Не хватает могущества? Сходите забаньте руками.")
            );
        }

        return false;
    }

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";
} 