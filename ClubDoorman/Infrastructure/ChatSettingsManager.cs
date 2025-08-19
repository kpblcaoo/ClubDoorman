using System.Text.Encodings.Web;

namespace ClubDoorman.Infrastructure;

public static class ChatSettingsManager
{
    private static readonly string SettingsPath = Path.Combine("data", "chat_settings.json");
    private static DateTime _lastReadTime = DateTime.MinValue;
    private static Dictionary<long, Dictionary<string, string>> _chatSettings = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public static string GetChatType(long chatId)
    {
        ReloadIfNeeded();
        return _chatSettings.TryGetValue(chatId, out var dict) && dict.TryGetValue("type", out var type) ? type : "default";
    }

    public static string? GetChatTitle(long chatId)
    {
        ReloadIfNeeded();
        return _chatSettings.TryGetValue(chatId, out var dict) && dict.TryGetValue("title", out var title) ? title : null;
    }

    public static void EnsureChatInConfig(long chatId, string? chatTitle)
    {
        ReloadIfNeeded();
        var changed = false;
        if (!_chatSettings.ContainsKey(chatId))
        {
            _chatSettings[chatId] = new Dictionary<string, string> { { "type", "default" }, { "title", chatTitle ?? "" } };
            changed = true;
        }
        else if (!string.IsNullOrEmpty(chatTitle) && _chatSettings[chatId].GetValueOrDefault("title") != chatTitle)
        {
            _chatSettings[chatId]["title"] = chatTitle;
            changed = true;
        }
        if (changed)
        {
            try
            {
                var dict = _chatSettings.ToDictionary(
                    kv => kv.Key.ToString(),
                    kv => kv.Value
                );
                var json = System.Text.Json.JsonSerializer.Serialize(
                    dict,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }
                );
                File.WriteAllText(SettingsPath, json);
            }
            catch { /* ignore */ }
        }
    }

    private static void ReloadIfNeeded()
    {
        if ((DateTime.UtcNow - _lastReadTime) < CacheDuration)
            return;
        _lastReadTime = DateTime.UtcNow;
        try
        {
            if (!File.Exists(SettingsPath))
            {
                _chatSettings = new();
                return;
            }
            var json = File.ReadAllText(SettingsPath);
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            _chatSettings = dict?.ToDictionary(
                kv => long.Parse(kv.Key),
                kv => kv.Value
            ) ?? new();
        }
        catch
        {
            _chatSettings = new();
        }
    }

    public static void InitConfigFileIfMissing()
    {
        if (!File.Exists(SettingsPath))
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, "{}\n");
            }
            catch { /* ignore */ }
        }
    }
}
