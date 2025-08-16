using Telegram.Bot.Types;
using ClubDoorman.TestInfrastructure;

namespace ClubDoorman.Test.TestInfrastructure;

/// <summary>
/// Shared admin/user test constants and helpers to keep BDD step definitions DRY.
/// </summary>
public static class TestAdmin
{
    public const long BotId = 123456789;              // Mirrors FakeTelegramClient.BotId
    public const long AdminUserId = 223456789;        // Distinct admin user id (must differ from BotId)
    public const long NonAdminUserId = 987654321;     // Regular user id for negative permission tests
    public const long AdminChatId = 123456789;        // Primary admin chat id

    /// <summary>
    /// Applies a standard set of administrators to the provided fake client (owner + real admin user).
    /// </summary>
    public static void ApplyStandardAdmins(FakeTelegramClient fake)
    {
        fake.SetupChatAdministrators(AdminChatId,
            new ChatMemberOwner { User = new User { Id = 1, FirstName = "Owner", Username = "owner" } },
            new ChatMemberAdministrator
            {
                User = new User { Id = AdminUserId, FirstName = "AdminUser", Username = "admin" },
                IsAnonymous = false,
                CanManageChat = true,
                CanDeleteMessages = true,
                CanManageVideoChats = true,
                CanRestrictMembers = true,
                CanPromoteMembers = true,
                CanChangeInfo = true,
                CanInviteUsers = true,
                CustomTitle = "Admin"
            });
    }
}
