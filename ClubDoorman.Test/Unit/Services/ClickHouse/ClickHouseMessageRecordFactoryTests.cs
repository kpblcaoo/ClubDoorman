using System;
using ClubDoorman.Services.ClickHouse;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Services.ClickHouse;

[TestFixture]
public class ClickHouseMessageRecordFactoryTests
{
    [Test]
    public void TryCreate_TextMessage_ProducesRecord()
    {
        var msg = new Message
        {
            Date = DateTime.UtcNow.AddMinutes(-1),
            Chat = new Chat { Id = -1001, Type = ChatType.Supergroup, Title = "Test" },
            From = new User { Id = 777, IsBot = false, FirstName = "Tester" },
            Text = "Hello world"
        };

        var options = new ClickHouseOptions { IngestSource = "live" };
        var created = ClickHouseMessageRecordFactory.TryCreate(msg, options, out var record);

        Assert.That(created, Is.True);
    Assert.That(record.ChatId, Is.EqualTo(msg.Chat.Id));
    Assert.That(record.MessageId, Is.EqualTo(msg.MessageId));
        Assert.That(record.FromId, Is.EqualTo(msg.From!.Id));
        Assert.That(record.TextLength, Is.EqualTo((ushort)msg.Text!.Length));
        Assert.That(record.HasUrl, Is.EqualTo((byte)0));
        Assert.That(record.HasMedia, Is.EqualTo((byte)0));
        Assert.That(record.IngestSource, Is.EqualTo("live"));
    }

    [Test]
    public void TryCreate_PrivateChatExcluded_ReturnsFalse()
    {
        var msg = new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 500, Type = ChatType.Private },
            From = new User { Id = 123, IsBot = false },
            Text = "Private message"
        };

        var options = new ClickHouseOptions { IncludePrivateChats = false };
        var created = ClickHouseMessageRecordFactory.TryCreate(msg, options, out _);

        Assert.That(created, Is.False);
    }

    [Test]
    public void TryCreate_MessageWithEntities_SetsFlags()
    {
        var msg = new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = -12, Type = ChatType.Group },
            From = new User { Id = 88, IsBot = false },
            Text = "Check https://example.com",
            Entities = new[]
            {
                new MessageEntity
                {
                    Type = MessageEntityType.Url,
                    Offset = 6,
                    Length = 19
                }
            },
            Photo = new[] { new PhotoSize { Height = 10, Width = 10, FileId = "1", FileUniqueId = "u", FileSize = 100 } }
        };

        var options = new ClickHouseOptions();
        var created = ClickHouseMessageRecordFactory.TryCreate(msg, options, out var record);

        Assert.That(created, Is.True);
        Assert.That(record.HasUrl, Is.EqualTo((byte)1));
        Assert.That(record.HasMedia, Is.EqualTo((byte)1));
    }
}
