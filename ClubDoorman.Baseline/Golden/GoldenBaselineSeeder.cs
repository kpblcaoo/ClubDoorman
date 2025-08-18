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
        var updates = new[]
        {
            Make(1, 900000001, "Baseline message one"),                   // neutral (was greeting)
            Make(2, 900000002, "Second baseline message about normal workflow"), // normal -> Allow
            Make(3, 900000003, "ищу партнеров для удаленного заработка"),   // stop-words -> Delete
            Make(4, 900000004, "Заходите https://example.com супер"),       // link -> should Delete when filter enabled
            // Use 16 emojis + short tail, avoid greeting words to trigger emoji filter only
            Make(5, 900000005, "😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀 xx"), // emoji flood -> Delete (many emojis)
            Make(6, 900000006, "Пакет номер 42 обработан успешно")          // normal -> Allow
        };

        foreach (var u in updates)
        {
            await dispatcher.DispatchAsync(u, cancellationToken);
        }

        await Task.Delay(150, cancellationToken);
        logger.LogInformation("Golden baseline seeder: completed feed");
    }
}
