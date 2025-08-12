using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Tests.TestKit2.Builders;

public static class FakeTelegramUpdateBuilders
{
    public static Update NewMemberJoin(long chatId, long userId, string? username = null)
    {
        return new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = chatId, Type = ChatType.Supergroup },
                NewChatMembers = new[] { new User { Id = userId, Username = username } },
                Date = DateTime.UtcNow
            },
            UpdateId = 1
        };
    }

    public static Update ForwardedMessage(long chatId, long fromUserId, long forwardFromId, string? text = null)
    {
        return new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = chatId, Type = ChatType.Supergroup },
                From = new User { Id = fromUserId },
                ForwardFrom = new User { Id = forwardFromId },
                Text = text,
                Date = DateTime.UtcNow
            },
            UpdateId = 2
        };
    }

    public static Update ChannelPost(long chatId, string? text = null)
    {
        return new Update
        {
            ChannelPost = new Message
            {
                Chat = new Chat { Id = chatId, Type = ChatType.Channel },
                Text = text,
                Date = DateTime.UtcNow
            },
            UpdateId = 3
        };
    }

    public static Update EditedMessage(long chatId, long userId, string? newText = null)
    {
        return new Update
        {
            EditedMessage = new Message
            {
                Chat = new Chat { Id = chatId, Type = ChatType.Supergroup },
                From = new User { Id = userId },
                Text = newText,
                Date = DateTime.UtcNow
            },
            UpdateId = 4
        };
    }

    public static Update MediaWithCaption(long chatId, long userId, string? caption = null)
    {
        return new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = chatId, Type = ChatType.Supergroup },
                From = new User { Id = userId },
                Caption = caption,
                Photo = new[] { new PhotoSize { FileId = "photoId" } },
                Date = DateTime.UtcNow
            },
            UpdateId = 5
        };
    }

    public static Update PrivateChatCommand(long userId, string command)
    {
        return new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = userId, Type = ChatType.Private },
                From = new User { Id = userId },
                Text = command,
                Entities = new[] { new MessageEntity { Type = MessageEntityType.BotCommand, Offset = 0, Length = command.Length } },
                Date = DateTime.UtcNow
            },
            UpdateId = 6
        };
    }
}
