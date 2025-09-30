using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services.ClickHouse;

/// <summary>
/// Produces ClickHouse-ready records from Telegram messages.
/// </summary>
public static class ClickHouseMessageRecordFactory
{
    private static readonly MessageEntityType[] UrlEntityTypes =
    {
        MessageEntityType.TextLink,
        MessageEntityType.Url,
        MessageEntityType.Mention,
        MessageEntityType.Email
    };

    private static readonly Regex UrlFallbackRegex = new("(https?:\\/\\/|t\\.me/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Builds a ClickHouse message record if the message should be logged.
    /// </summary>
    public static bool TryCreate(Message message, ClickHouseOptions options, out ClickHouseMessageRecord record)
    {
        record = default;
        if (message == null) return false;
        if (message.Chat == null) return false;
        if (!options.IncludePrivateChats && message.Chat.Type == ChatType.Private) return false;

        var hasMedia = HasMedia(message);
        var text = message.Text ?? message.Caption ?? string.Empty;
        if (!hasMedia && string.IsNullOrEmpty(text))
        {
            return false; // service messages with no payload
        }

        var eventTs = message.Date;
        if (eventTs.Kind == DateTimeKind.Unspecified)
        {
            eventTs = DateTime.SpecifyKind(eventTs, DateTimeKind.Utc);
        }
        else if (eventTs.Kind == DateTimeKind.Local)
        {
            eventTs = eventTs.ToUniversalTime();
        }

        var ingestTs = DateTime.UtcNow;
        var chatType = message.Chat.Type.ToString().ToLowerInvariant();
        var fromId = message.From?.Id ?? message.SenderChat?.Id ?? 0;
        var fromIsBot = (byte)((message.From?.IsBot ?? (message.SenderChat != null)) ? 1 : 0);
        var textLength = (ushort)Math.Min(text.Length, ushort.MaxValue);
        var hasUrl = (byte)(ContainsUrl(message, text) ? 1 : 0);
        var replyTo = message.ReplyToMessage?.MessageId ?? 0;

        record = new ClickHouseMessageRecord(
            EventTs: eventTs,
            IngestTs: ingestTs,
            ChatId: message.Chat.Id,
            ChatType: chatType,
            MessageId: message.MessageId,
            FromId: fromId,
            FromIsBot: fromIsBot,
            TextLength: textLength,
            HasUrl: hasUrl,
            HasMedia: (byte)(hasMedia ? 1 : 0),
            ReplyToId: replyTo,
            IngestSource: options.IngestSource
        );

        return true;
    }

    private static bool ContainsUrl(Message message, string text)
    {
        if (message.Entities != null && message.Entities.Any(e => UrlEntityTypes.Contains(e.Type)))
        {
            return true;
        }

        if (message.CaptionEntities != null && message.CaptionEntities.Any(e => UrlEntityTypes.Contains(e.Type)))
        {
            return true;
        }

        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return UrlFallbackRegex.IsMatch(text);
    }

    private static bool HasMedia(Message message)
    {
        return (message.Photo != null && message.Photo.Length > 0)
            || message.Document != null
            || message.Audio != null
            || message.Video != null
            || message.Animation != null
            || message.Sticker != null
            || message.VideoNote != null
            || message.Voice != null
            || message.Contact != null
            || message.Dice != null
            || message.Game != null
            || message.Invoice != null
            || message.Location != null
            || message.Poll != null
            || message.Story != null
            || message.SuccessfulPayment != null
            || message.Venue != null;
    }
}
