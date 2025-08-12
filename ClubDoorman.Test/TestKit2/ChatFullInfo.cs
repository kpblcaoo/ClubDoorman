using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Tests.TestKit2;

public class ChatFullInfo
{
    public long Id { get; set; }
    public ChatType Type { get; set; }
    public string? Title { get; set; }
    public string? Username { get; set; }
    public string? Bio { get; set; }
    public long? LinkedChatId { get; set; }
    public ChatPhoto? Photo { get; set; }
}
