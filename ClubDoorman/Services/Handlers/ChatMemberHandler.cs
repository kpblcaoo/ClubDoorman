using System.Diagnostics;
using System.Runtime.Caching;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Handlers;
using ClubDoorman.Services.UserJoin;
using ClubDoorman.Services.Logging;
using ClubDoorman.Models.Logging;
using Microsoft.Extensions.Options;

namespace ClubDoorman.Services.Handlers;

/// <summary>
/// Обработчик изменений участников чата
/// </summary>
public class ChatMemberHandler : IUpdateHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IUserManager _userManager;
    private readonly ILogger<ChatMemberHandler> _logger;
    private readonly IntroFlowService _introFlowService;
    private readonly IMessageService _messageService;
    private readonly IAppConfig _appConfig;
    private readonly IUserCleanupService _userCleanupService;
    private readonly IFolderInviteService _folderInviteService;
    private readonly LoggingFlags _loggingFlags;

    public ChatMemberHandler(
        ITelegramBotClientWrapper bot,
        IUserManager userManager,
        ILogger<ChatMemberHandler> logger,
        IntroFlowService introFlowService,
        IMessageService messageService,
        IAppConfig appConfig,
        IUserCleanupService userCleanupService,
        IFolderInviteService folderInviteService,
        IOptions<LoggingFlags> loggingFlags)
    {
        _bot = bot;
        _userManager = userManager;
        _logger = logger;
        _introFlowService = introFlowService;
        _messageService = messageService;
        _appConfig = appConfig;
        _userCleanupService = userCleanupService;
        _folderInviteService = folderInviteService;
        _loggingFlags = loggingFlags?.Value ?? throw new ArgumentNullException(nameof(loggingFlags));
    }

    public bool CanHandle(Update update) => update.Type == UpdateType.ChatMember;

    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        var chatMember = update.ChatMember;
        Debug.Assert(chatMember != null);
        
        // Начинаем корреляционный scope для трейсинга
        using var correlationScope = _logger.BeginCorrelationScope(null, chatMember.Chat.Id);
        
        // Трейс: входная точка
        _logger.LogTrace(_loggingFlags, "Routed->ChatMemberHandler", new { chatId = chatMember.Chat.Id, userId = chatMember.NewChatMember.User.Id });

        // Golden Master: сохраняем входные данные
        var inputData = new { update = update, timestamp = DateTime.UtcNow };
        object? outputData = null;

        try
        {
            await HandleChatMemberInternalAsync(chatMember, cancellationToken);
            
            // Golden Master: успешное завершение
            outputData = new { success = true, timestamp = DateTime.UtcNow };
            _logger.LogTrace(_loggingFlags, "ChatMemberHandler->Completed", new { chatId = chatMember.Chat.Id });
        }
        catch (Exception ex)
        {
            // Golden Master: ошибка
            outputData = new { success = false, error = ex.Message, timestamp = DateTime.UtcNow };
            _logger.LogTrace(_loggingFlags, "ChatMemberHandler->Error", new { chatId = chatMember.Chat.Id, error = ex.Message });
            throw;
        }
        finally
        {
            // Записываем Golden Master файл (используем chatId как messageId для уникальности)
            if (_loggingFlags.GoldenMasterEnabled)
            {
                await GoldenMasterRecorder.RecordAsync(inputData, outputData, "ChatMemberHandler", chatMember.Chat.Id, _loggingFlags, _logger);
            }
        }
    }

    /// <summary>
    /// Внутренняя логика обработки изменения участника (вынесена для Golden Master)
    /// </summary>
    private async Task HandleChatMemberInternalAsync(ChatMemberUpdated chatMember, CancellationToken cancellationToken)
    {
        var newChatMember = chatMember.NewChatMember;
        ChatSettingsManager.EnsureChatInConfig(chatMember.Chat.Id, chatMember.Chat.Title);
        
        // Проверка whitelist - если активен, работаем только в разрешённых чатах
        if (!_appConfig.IsChatAllowed(chatMember.Chat.Id))
        {
            _logger.LogDebug("Чат {ChatId} ({ChatTitle}) не в whitelist - игнорируем изменение участника", chatMember.Chat.Id, chatMember.Chat.Title);
            return;
        }
        
        // Игнорируем изменения, сделанные самим ботом
        if (chatMember.From?.Id == _bot.BotId)
        {
            _logger.LogDebug("Игнорируем изменение статуса участника, сделанное самим ботом");
            return;
        }
        
        switch (newChatMember.Status)
        {
            case ChatMemberStatus.Member:
            {
                _logger.LogTrace(_loggingFlags, "ChatMemberHandler->NewMember", new { chatId = chatMember.Chat.Id, userId = newChatMember.User.Id });
                _logger.LogDebug("New chat member new {@New} old {@Old}", newChatMember, chatMember.OldChatMember);
                if (chatMember.OldChatMember.Status == ChatMemberStatus.Left)
                {
                    var u = newChatMember.User;
                    _logger.LogInformation("==================== НОВЫЙ УЧАСТНИК ====================\nПользователь {User} (id={UserId}, username={Username}) зашел в группу '{ChatTitle}' (id={ChatId})\n========================================================", 
                        (u.FirstName + (string.IsNullOrEmpty(u.LastName) ? "" : " " + u.LastName)), u.Id, u.Username ?? "-", chatMember.Chat.Title ?? "-", chatMember.Chat.Id);
                    
                    // Проверяем вход через папку
                    var wasBannedForFolderInvite = await _folderInviteService.HandleFolderInviteAsync(chatMember, cancellationToken);
                    
                    // Если пользователь не был забанен за вход через папку, запускаем обычный IntroFlow
                    if (!wasBannedForFolderInvite)
                    {
                        _logger.LogTrace(_loggingFlags, "ChatMemberHandler->IntroFlow", new { chatId = chatMember.Chat.Id, userId = newChatMember.User.Id });
                        // Запускаем IntroFlow через сервис
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(TimeSpan.FromSeconds(2));
                            await _introFlowService.ProcessNewUserAsync(null, newChatMember.User, chatMember.Chat);
                        });
                    }
                    else
                    {
                        _logger.LogTrace(_loggingFlags, "ChatMemberHandler->FolderBan", new { chatId = chatMember.Chat.Id, userId = newChatMember.User.Id });
                    }
                }
                break;
            }
            case ChatMemberStatus.Kicked
            or ChatMemberStatus.Restricted:
                _logger.LogTrace(_loggingFlags, "ChatMemberHandler->Restricted", new { chatId = chatMember.Chat.Id, userId = newChatMember.User.Id, status = newChatMember.Status });
                var user = newChatMember.User;
                var key = $"{chatMember.Chat.Id}_{user.Id}";
                var lastMessage = MemoryCache.Default.Get(key) as string;
                var tailMessage = string.IsNullOrWhiteSpace(lastMessage)
                    ? ""
                    : $" Его/её последним сообщением было:\n```\n{lastMessage}\n```";
                
                // Удаляем из списка доверенных
                if (_userCleanupService.RemoveUserFromAllApprovals(user.Id, "Получение ограничений"))
                {
                    _logger.LogTrace(_loggingFlags, "ChatMemberHandler->RemovedFromApproved", new { chatId = chatMember.Chat.Id, userId = newChatMember.User.Id });
                    var removedData = new UserRemovedFromApprovedNotificationData(
                        user, chatMember.Chat, "удален из списка одобренных после получения ограничений", 0, chatMember.Chat.Title ?? "");
                    await _messageService.SendAdminNotificationAsync(
                        AdminNotificationType.UserRemovedFromApproved,
                        removedData,
                        cancellationToken
                    );
                }
                
                var restrictedData = new UserRestrictedNotificationData(
                    user, chatMember.Chat, "пользователь получил ограничения", 0, lastMessage, chatMember.Chat.Title ?? "");
                await _messageService.SendAdminNotificationAsync(
                    AdminNotificationType.UserRestricted,
                    restrictedData,
                    cancellationToken
                );
                break;
        }
    }

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";
} 