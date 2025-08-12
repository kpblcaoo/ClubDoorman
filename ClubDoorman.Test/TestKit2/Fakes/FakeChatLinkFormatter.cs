using ClubDoorman.Services.Messaging;
using Telegram.Bot.Types;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeChatLinkFormatter : IChatLinkFormatter
{
    public string GetChatLink(Chat chat)
    {
        return $"**{chat.Title ?? "Unknown Chat"}**";
    }
    
    public string GetChatLink(long chatId, string? chatTitle)
    {
        return $"**{chatTitle ?? "Unknown Chat"}**";
    }
}
