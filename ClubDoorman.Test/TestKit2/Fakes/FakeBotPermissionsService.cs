using ClubDoorman.Services;
using Telegram.Bot.Types;

namespace ClubDoorman.Tests.TestKit2.Fakes;

/// <summary>
/// Фейк для IBotPermissionsService
/// </summary>
public class FakeBotPermissionsService : IBotPermissionsService
{
    public bool IsSilentMode { get; set; } = false;
    public bool IsBotAdmin { get; set; } = true;
    public List<long> SilentModeChats { get; } = new();
    public List<long> AdminChats { get; } = new();

    public Task<bool> IsBotAdminAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsBotAdmin || AdminChats.Contains(chatId));
    }

    public Task<bool> IsSilentModeAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsSilentMode || SilentModeChats.Contains(chatId));
    }

    public Task<ChatMember?> GetBotChatMemberAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var isAdmin = IsBotAdmin || AdminChats.Contains(chatId);
        if (isAdmin)
        {
                    return Task.FromResult<ChatMember?>(new ChatMemberAdministrator
        {
            User = new Telegram.Bot.Types.User { Id = 123456789, IsBot = true, FirstName = "TestBot" },
            CanDeleteMessages = true,
            CanRestrictMembers = true
        });
        }
        return Task.FromResult<ChatMember?>(null);
    }

    public void SetSilentMode(bool isSilentMode)
    {
        IsSilentMode = isSilentMode;
    }

    public void AddSilentModeChat(long chatId)
    {
        SilentModeChats.Add(chatId);
    }

    public void RemoveSilentModeChat(long chatId)
    {
        SilentModeChats.Remove(chatId);
    }

    public void SetBotAdmin(bool isBotAdmin)
    {
        IsBotAdmin = isBotAdmin;
    }

    public void AddAdminChat(long chatId)
    {
        AdminChats.Add(chatId);
    }

    public void RemoveAdminChat(long chatId)
    {
        AdminChats.Remove(chatId);
    }
} 