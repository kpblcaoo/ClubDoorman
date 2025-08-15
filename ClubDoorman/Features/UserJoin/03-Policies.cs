using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.TextProcessing;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Models.Requests;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;

namespace ClubDoorman.Features.UserJoin;

/// <summary>
/// Политика для обработки присоединения пользователей
/// <tags>user-join, policy, decisions, logic, implementation</tags>
/// </summary>
public class UserJoinPolicy : IUserJoinPolicy
{
    private readonly IModerationService _moderationService;
    private readonly IUserBanService _userBanService;
    private readonly IUserManager _userManager;
    private readonly ICaptchaService _captchaService;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IAppConfig _appConfig;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly GlobalStatsManager _globalStatsManager;
    private readonly IAiChecks _aiChecks;
    private readonly IMessageService _messageService;
    private readonly IJoinedUserFlags _joinedUserFlags;
    private readonly ILogger<UserJoinPolicy> _logger;

    public UserJoinPolicy(
        IModerationService moderationService,
        IUserBanService userBanService,
        IUserManager userManager,
        ICaptchaService captchaService,
        IUserFlowLogger userFlowLogger,
        IAppConfig appConfig,
        ITelegramBotClientWrapper bot,
        GlobalStatsManager globalStatsManager,
        IAiChecks aiChecks,
        IMessageService messageService,
        IJoinedUserFlags joinedUserFlags,
        ILogger<UserJoinPolicy> logger)
    {
        _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
        _userBanService = userBanService ?? throw new ArgumentNullException(nameof(userBanService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        _userFlowLogger = userFlowLogger ?? throw new ArgumentNullException(nameof(userFlowLogger));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _globalStatsManager = globalStatsManager ?? throw new ArgumentNullException(nameof(globalStatsManager));
        _aiChecks = aiChecks ?? throw new ArgumentNullException(nameof(aiChecks));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _joinedUserFlags = joinedUserFlags ?? throw new ArgumentNullException(nameof(joinedUserFlags));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Обрабатывает присоединение новых пользователей
    /// <tags>user-join, new-members, processing</tags>
    /// </summary>
    /// <param name="message">Сообщение о новых участниках</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task HandleNewMembersAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.NewChatMembers == null)
        {
            _logger.LogDebug("Сообщение о новых участниках не содержит данных о пользователях");
            return;
        }

        foreach (var newUser in message.NewChatMembers.Where(x => x != null && !x.IsBot))
        {
            if (!_joinedUserFlags.IsUserRecentlyJoined(message.Chat.Id, newUser.Id))
            {
                _logger.LogInformation("==================== НОВЫЙ УЧАСТНИК ====================\n" +
                    "Пользователь {User} (id={UserId}, username={Username}) зашел в группу '{ChatTitle}' (id={ChatId})\n" +
                    "========================================================",
                    Utils.FullName(newUser), newUser.Id, newUser.Username ?? "-", message.Chat.Title ?? "-", message.Chat.Id);

                _joinedUserFlags.MarkUserAsJoined(message.Chat.Id, newUser.Id);
            }

            await ProcessNewUserAsync(message, newUser, cancellationToken);
        }
    }

    /// <summary>
    /// Обрабатывает одного нового пользователя
    /// <tags>user-join, single-user, processing</tags>
    /// </summary>
    /// <param name="userJoinMessage">Сообщение о присоединении</param>
    /// <param name="user">Пользователь</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task ProcessNewUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            _logger.LogWarning("ProcessNewUserAsync вызван с null пользователем");
            return;
        }

        if (userJoinMessage?.Chat == null)
        {
            _logger.LogWarning("ProcessNewUserAsync вызван с null сообщением или чатом");
            return;
        }

        var chat = userJoinMessage.Chat;

        // Проверка имени пользователя
        if (_moderationService == null)
        {
            _logger.LogError("_moderationService равен null в ProcessNewUserAsync");
            return;
        }

        var nameResult = await _moderationService.CheckUserNameAsync(user);
        if (nameResult.Action == ModerationAction.Ban)
        {
            await _userBanService.BanUserForLongNameAsync(userJoinMessage, user, nameResult.Reason, null, cancellationToken);
            return;
        }
        if (nameResult.Action == ModerationAction.Report)
        {
            await _userBanService.BanUserForLongNameAsync(userJoinMessage, user, nameResult.Reason, TimeSpan.FromMinutes(10), cancellationToken);
            return;
        }

        // Проверка клубного пользователя
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (clubUser != null)
        {
            _logger.LogDebug("User is {Name} from club", clubUser);
            return;
        }

        // Проверка блэклиста
        if (await _userManager.InBanlist(user.Id))
        {
            await _userBanService.BanBlacklistedUserAsync(userJoinMessage, user, cancellationToken);
            return;
        }

        // Проверяем, не находится ли пользователь уже в процессе прохождения капчи
        var captchaKey = _captchaService.GenerateKey(chat.Id, user.Id);
        if (_captchaService.GetCaptchaInfo(captchaKey) != null)
        {
            _logger.LogDebug("Пользователь уже проходит капчу");
            return;
        }

        // Создаем капчу
        var request = new CreateCaptchaRequest(chat, user, userJoinMessage);
        var captchaInfo = await _captchaService.CreateCaptchaAsync(request);
        if (captchaInfo == null)
        {
            _logger.LogInformation($"[NO_CAPTCHA] Капча не требуется для чата {chat.Id}");
            return;
        }
    }
}
