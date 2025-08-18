using System.Security.Cryptography;
using System.Text;
using ClubDoorman.Models.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// Записывает Golden Master сэмплы (input/output) с канонизацией и сэмплированием.
/// </summary>
public class GoldenMasterRecorder : IGoldenMasterRecorder
{
    private readonly IOptions<LoggingFlagsOptions> _flags;
    private readonly ILogger<GoldenMasterRecorder> _logger;
    private readonly double _sampleRate;
    private readonly Random _rng = new();

    public GoldenMasterRecorder(IOptions<LoggingFlagsOptions> flags, ILogger<GoldenMasterRecorder> logger)
    {
        _flags = flags;
        _logger = logger;
        _sampleRate = Math.Clamp(_flags.Value.GoldenSampleRate, 0, 1);
    }

    public string? TryRecordInput(Update update, string handlerName, long? chatId, long? userId)
    {
        var f = _flags.Value;
        if (!f.GoldenMasterEnabled) return null;
        if (_sampleRate <= 0) return null;
        if (_rng.NextDouble() > _sampleRate) return null;

        var correlationId = GenerateCorrelationId(update, handlerName);
        try
        {
            var simplified = SimplifyUpdate(update);
            // Mask raw user id to avoid leaking PII into Golden Master snapshots (test enforces this)
            string? maskedUserId = null;
            if (userId.HasValue)
            {
                maskedUserId = "U" + Math.Abs(userId.Value % 10000).ToString("D4");
            }
            var sanitized = Canonicalize(new
            {
                Type = update.Type.ToString(),
                ChatId = chatId,
                UserId = maskedUserId,
                handler = handlerName,
                Payload = simplified
            });
            WriteFile(correlationId, "input", sanitized);
            return correlationId;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GoldenMasterRecorder: failed to record input");
            return null; // fail silent
        }
    }

    public void TryRecordOutput(string? correlationId, object? resultPayload)
    {
        if (correlationId == null) return;
        var f = _flags.Value;
        if (!f.GoldenMasterEnabled) return;
        try
        {
            var enriched = EnrichOutput(resultPayload);
            var sanitized = Canonicalize(new { Output = enriched });
            WriteFile(correlationId, "output", sanitized);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GoldenMasterRecorder: failed to record output");
        }
    }

    private static object SimplifyUpdate(Update u)
    {
        string MaskUserId(long id) => "U" + Math.Abs(id % 10000).ToString("D4");
        string? MaskUsername(string? username) => string.IsNullOrEmpty(username) ? null : ("u_" + username.GetHashCode().ToString("x"));
        string? mediaKind = null;
        var hasText = !string.IsNullOrWhiteSpace(u.Message?.Text) || !string.IsNullOrWhiteSpace(u.Message?.Caption);
        if (u.Message != null && !hasText)
        {
            if (u.Message.Photo != null && u.Message.Photo.Any()) mediaKind = "photo";
            else if (u.Message.Video != null) mediaKind = "video";
            else if (u.Message.Sticker != null) mediaKind = "sticker";
            else if (u.Message.Document != null) mediaKind = "document";
        }
        return new
        {
            u.Id,
            Type = u.Type.ToString(),
            Message = u.Message == null ? null : new
            {
                u.Message.MessageId,
                ChatId = u.Message.Chat.Id,
                u.Message.Chat.Type,
                u.Message.Chat.Title,
                From = u.Message.From == null ? null : new { Id = MaskUserId(u.Message.From.Id), u.Message.From.IsBot, Username = MaskUsername(u.Message.From.Username) },
                Text = Truncate(u.Message.Text ?? u.Message.Caption, 160),
                MediaKind = mediaKind
            },
            ChatMember = u.ChatMember == null ? null : new
            {
                ChatId = u.ChatMember.Chat.Id,
                u.ChatMember.Chat.Title,
                NewStatus = u.ChatMember.NewChatMember.Status.ToString(),
                User = new { Id = MaskUserId(u.ChatMember.NewChatMember.User.Id), Username = MaskUsername(u.ChatMember.NewChatMember.User.Username) }
            }
        };
    }

    private static object? EnrichOutput(object? payload)
    {
        if (payload == null) return null;
        try
        {
            var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            });
            var token = JToken.Parse(json);
            if (token is JObject obj)
            {
                var reason = obj.Property("reason")?.Value?.ToString();
                var kind = obj.Property("kind")?.Value?.ToString();
                var code = ReasonCodeMapper.MapReason(reason, kind);
                if (obj.Property("ruleCode") == null)
                {
                    obj.Add("ruleCode", code.ToString());
                }
                return obj;
            }
            return token;
        }
        catch
        {
            return payload;
        }
    }

    private static string Truncate(string? s, int max) => string.IsNullOrEmpty(s) ? s ?? "" : (s.Length <= max ? s : s.Substring(0, max));

    private static object Canonicalize(object payload)
    {
        // Стабилизация и очистка: удаляем null, сортируем свойства по алфавиту для детерминированного diff.
        var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        });
        var token = JToken.Parse(json);
        SortToken(token);
        return token; // JObject/JArray вернёт отсортированный детерминированный вид
    }

    private static void SortToken(JToken token)
    {
        switch (token)
        {
            case JObject obj:
                // Сортировка свойств
                var props = obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
                foreach (var p in props)
                {
                    p.Remove();
                }
                foreach (var p in props)
                {
                    obj.Add(p.Name, p.Value);
                    SortToken(p.Value);
                }
                break;
            case JArray arr:
                foreach (var item in arr)
                {
                    SortToken(item);
                }
                break;
        }
    }

    private string GenerateCorrelationId(Update update, string handler)
    {
        var f = _flags.Value;
        if (f.GoldenDeterministicIds)
        {
            // Детерминированный id только на основе стабильных свойств.
            var rawStable = $"{update.Id}:{handler}:{update.Type}:{update.Message?.Chat.Id}:{update.Message?.From?.Id}";
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawStable))).Substring(0, 16);
        }
        var raw = $"{update.Id}:{handler}:{DateTime.UtcNow:yyyyMMddHHmmssfff}:{Guid.NewGuid()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).Substring(0, 16);
    }

    private void WriteFile(string correlationId, string phase, object data)
    {
        var f = _flags.Value;
        var dateFolder = f.GoldenFixedDateFolder ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        var basePath = f.GoldenBasePath ?? "golden";
        var dir = Path.Combine(basePath, dateFolder);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{correlationId}.{phase}.json");
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}
