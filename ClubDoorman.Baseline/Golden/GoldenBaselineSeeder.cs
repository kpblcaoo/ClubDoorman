using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Services;

namespace ClubDoorman.Baseline.Golden;

internal sealed class GoldenBaselineSeeder(ILogger<GoldenBaselineSeeder> logger, IUpdateDispatcher dispatcher) : IGoldenBaselineSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Golden baseline seeder: start feed");
        var now = DateTime.UtcNow;
        // Use regular group chat type (not Supergroup/Channel) so emoji flood policy applies identically to production expectations
        // Force chat type to Group (default semantic) but also ensure downstream lookups treat it as default by not relying on chat_settings.json
        var chat = new Chat { Id = -1001234567890, Title = "GoldenBaselineChat", Type = Telegram.Bot.Types.Enums.ChatType.Group };

        // Additional chat configured as 'announcement' (through chat_settings.json) to simulate media filtering disabled effect for ONE scenario.
        // This allows us to have a contrasting snapshot (Report instead of Delete) without toggling global env flags for entire baseline.
        var announcementChat = new Chat { Id = -1001234567891, Title = "GoldenAnnouncementChat", Type = Telegram.Bot.Types.Enums.ChatType.Group };
        try
        {
            var settingsPath = Path.Combine("data", "chat_settings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            // Build a minimal settings dictionary preserving default chat and marking second chat as announcement
            var dict = new Dictionary<string, Dictionary<string, string>>
            {
                [chat.Id.ToString()] = new() { { "type", "default" }, { "title", chat.Title ?? "" } },
                [announcementChat.Id.ToString()] = new() { { "type", "announcement" }, { "title", announcementChat.Title ?? "" } }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(dict, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(settingsPath, json);
            logger.LogInformation("Pre-seeded chat_settings.json with announcement chat for selective media scenario");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to pre-seed chat_settings.json; announcement media scenario may not behave as expected");
        }

        // Distinct users per scenario to avoid early global approval skipping later moderation (e.g. links)
        User MakeUser(long id) => new() { Id = id, IsBot = false, Username = $"baseline_user_{id}", FirstName = "Baseline", LastName = "User" };
        Message CreateMsg(User u, string text) => new() { Date = now, Chat = chat, From = u, Text = text };
        Update Make(int id, long userId, string text) => new() { Id = id, Message = CreateMsg(MakeUser(userId), text) };

        // Deterministic scenario set:
        // 1: Previously greeting; now tweak to avoid boring greeting deletion to keep only dedicated greeting scenarios if needed.
        // 2: Normal informative message -> Allow
        // 3: Stop-words / earning spam phrase -> expect Delete/Report depending on rules
        // 4: Message containing a link -> expect Delete/Report (link policy)
        // 5: Emoji flood (no greeting word) -> expect Delete (В этом сообщении многовато эмоджи)
        // 6: Mixed benign content with number & cyrillic (control Allow)
        // Extended scenarios start at id 7
        // 7: Boring greeting to explicitly capture greeting deletion
        var boringGreetingUser = MakeUser(900000007);
        var greetingMsg = CreateMsg(boringGreetingUser, "Привет");
        var upd7 = new Update { Id = 7, Message = greetingMsg };

        // 8 & 9: Same user triggers two emoji floods to accumulate violations (ban threshold controlled by env -> set to 2 outside seeder)
        var emojiRepeatUser = MakeUser(900000008);
        string flood = "😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀 test"; // > 10 emojis
        var upd8 = new Update { Id = 8, Message = CreateMsg(emojiRepeatUser, flood) };
        var upd9 = new Update { Id = 9, Message = CreateMsg(emojiRepeatUser, flood) }; // second violation

        // 10: Mixed stop-word + link (priority check). Text contains stop phrase + URL
        var upd10 = Make(10, 900000009, "ищу партнеров https://spam.example для удаленного заработка");

        // 11: Media without text in default chat.
        // Expected Delete ("В первых трёх сообщениях нельзя отправлять картинки или видео").
        var mediaUser = MakeUser(900000010);
        var mediaMsg = new Message
        {
            Date = now,
            Chat = chat,
            From = mediaUser,
            Photo = new[] { new PhotoSize { FileId = "file-id-1", Width = 10, Height = 10, FileSize = 123 } },
            Caption = null,
            Text = null
        };
        var upd11 = new Update { Id = 11, Message = mediaMsg };

        // 12: Command /start from not-approved user (first message)
        var upd12 = Make(12, 900000011, "/start");

        // 13 & 14: Reply chain. First normal message allowed, second is a reply containing link (should still delete)
        var replyUser = MakeUser(900000012);
        var firstReplyBase = CreateMsg(replyUser, "Первое обычное сообщение цепочки");
        var upd13 = new Update { Id = 13, Message = firstReplyBase };
        var replyWithLink = CreateMsg(replyUser, "Ответ с ссылкой http://malicious.ru");
        replyWithLink.ReplyToMessage = firstReplyBase;
        var upd14 = new Update { Id = 14, Message = replyWithLink };

        // 15: User from banlist immediate reaction (user id chosen to match DOORMAN_TEST_BLACKLIST_IDS value set in Program)
        var banlistUser = MakeUser(900000050);
        var upd15 = new Update { Id = 15, Message = CreateMsg(banlistUser, "Сообщение от забаненного ранее пользователя") };

        // 16 & 17: Emoji threshold boundary (exactly at limit vs over). Build messages with 10 emojis (boundary) then 11.
        var boundaryUser = MakeUser(900000013);
        string tenEmojis = string.Concat(Enumerable.Repeat("😀", 10)) + " ok"; // boundary
        string elevenEmojis = string.Concat(Enumerable.Repeat("😀", 11)) + " boom"; // over limit
        var upd16 = new Update { Id = 16, Message = CreateMsg(boundaryUser, tenEmojis) };   // should be Allow (if policy counts >=10; adjust expectation accordingly)
        var upd17 = new Update { Id = 17, Message = CreateMsg(boundaryUser, elevenEmojis) }; // should Delete

        // 18: Media without text in announcement chat (single contrasting scenario -> should become Report "Медиа без подписи")
        var mediaAnnouncementUser = MakeUser(900000014);
        var mediaAnnouncementMsg = new Message
        {
            Date = now,
            Chat = announcementChat,
            From = mediaAnnouncementUser,
            Photo = new[] { new PhotoSize { FileId = "file-id-annc-1", Width = 10, Height = 10, FileSize = 111 } },
            Caption = null,
            Text = null
        };
        var upd18 = new Update { Id = 18, Message = mediaAnnouncementMsg };

        var updates = new List<Update>
        {
            Make(1, 900000001, "Baseline message one"),
            Make(2, 900000002, "Second baseline message about normal workflow"),
            Make(3, 900000003, "ищу партнеров для удаленного заработка"),
            Make(4, 900000004, "Заходите https://example.com супер"),
            Make(5, 900000005, "😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀 xx"),
            Make(6, 900000006, "Пакет номер 42 обработан успешно"),
            upd7, upd8, upd9, upd10, upd11, upd12, upd13, upd14, upd15, upd16, upd17
        };

        // Add selective feature-toggle scenario at the end (media in announcement chat -> manual review/report)
        updates.Add(upd18);

        foreach (var u in updates)
            await dispatcher.DispatchAsync(u, cancellationToken);

        await Task.Delay(150, cancellationToken);
        logger.LogInformation("Golden baseline seeder: completed feed");
    }
}
