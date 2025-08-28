using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace ClubDoorman.Baseline.Golden;

/// <summary>
/// Phase 2: Builds deterministic manifest for baseline snapshots.
/// </summary>
internal static class GoldenManifestBuilder
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly Dictionary<int, string> ShortNames = new()
    {
        [1] = "BaseMsg1",
        [2] = "BaseMsg2",
        [3] = "StopWords",
        [4] = "Link",
        [5] = "EmojiFlood",
        [6] = "BenignControl",
        [7] = "Greeting",
    // IDs 8 and 9 (EmojiFloodRepeat*) removed in Phase 7 aggregation (intentionally left unused to avoid shifting IDs)
        [10] = "MixedStopWordsLink",
        [11] = "MediaEarlyDefault",
        [12] = "CommandStart",
        [13] = "ReplyBase",
        [14] = "ReplyWithLink",
        [15] = "BanlistUser",
        [16] = "EmojiBoundaryOk",
    // ID 17 (EmojiBoundaryOver) removed in Phase 7 aggregation
        [18] = "MediaAnnouncement"
    };


    public static void Build(string goldenRoot, string variantName)
    {
        if (!string.Equals(variantName, "baseline", StringComparison.OrdinalIgnoreCase))
            return; // Only generate manifest once for primary variant

        var variantDir = Path.Combine(goldenRoot, variantName);
        if (!Directory.Exists(variantDir)) return;

        var inputs = Directory.GetFiles(variantDir, "*.input.json", SearchOption.TopDirectoryOnly);
        var entries = new List<ManifestEntry>();
        foreach (var inputPath in inputs)
        {
            try
            {
                var correlationId = Path.GetFileName(inputPath)!.Split('.')[0];
                // V1 output snapshot removed (Phase 9); rely solely on input + deterministic mapping.

                using var inputDoc = JsonDocument.Parse(File.ReadAllText(inputPath));
                var payload = inputDoc.RootElement.GetProperty("Payload");
                var updateId = payload.TryGetProperty("Id", out var idProp) && idProp.TryGetInt32(out var uid) ? uid : -1;
                if (updateId <= 0) continue;

                // Attempt to read semantics file (written by GoldenMasterRecorder) to populate action / ruleCode.
                string? ruleCode = null;
                string? expectedAction = null;
                var semPath = Path.Combine(variantDir, correlationId + ".sem.json");
                if (File.Exists(semPath))
                {
                    try
                    {
                        using var semDoc = JsonDocument.Parse(File.ReadAllText(semPath));
                        if (semDoc.RootElement.TryGetProperty("action", out var a)) expectedAction = a.GetString();
                        if (semDoc.RootElement.TryGetProperty("ruleCode", out var rc)) ruleCode = rc.GetString();
                    }
                    catch { /* ignore */ }
                }

                // No overrides: ExpectedAction / RuleCode may remain null if semantics not yet recorded (will surface in invariants/tests).

                ShortNames.TryGetValue(updateId, out var shortName);
                if (string.IsNullOrWhiteSpace(shortName))
                    shortName = "Scenario" + updateId;

                entries.Add(new ManifestEntry
                {
                    Id = updateId,
                    ShortName = shortName,
                    CorrelationId = correlationId,
                    ExpectedAction = expectedAction,
                    RuleCode = ruleCode
                });
            }
            catch
            {
                // non-fatal
            }
        }

        if (entries.Count == 0)
        {
            // Still write empty manifest for diagnostics.
            Console.WriteLine("[GoldenManifest] No entries discovered in variant directory: " + variantDir);
        }
        entries = entries.OrderBy(e => e.Id).ToList();

        // Deterministic timestamp support: allow fixed timestamp via env to stabilize golden diffs.
        // If DOORMAN_GOLDEN_FIXED_TIMESTAMP is set (ISO 8601), we use it; else fall back to current UTC.
        var fixedTsEnv = Environment.GetEnvironmentVariable("DOORMAN_GOLDEN_FIXED_TIMESTAMP");
        DateTime generatedAt;
        if (!string.IsNullOrWhiteSpace(fixedTsEnv) && DateTime.TryParse(fixedTsEnv, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            generatedAt = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }
        else
        {
            generatedAt = DateTime.UtcNow;
        }

        var manifest = new ManifestRoot
        {
            Schema = 1,
            GeneratedAtUtc = generatedAt,
            Variant = variantName,
            Entries = entries
        };

        var manifestPath = Path.Combine(goldenRoot, "manifest.json");
        var json = JsonSerializer.Serialize(manifest, Options);
        File.WriteAllText(manifestPath, json);
        Console.WriteLine($"[GoldenManifest] Written {entries.Count} entries to {manifestPath}");
    }

    private sealed class ManifestRoot
    {
        public int Schema { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public string Variant { get; set; } = "baseline";
        public List<ManifestEntry> Entries { get; set; } = new();
    }

    private sealed class ManifestEntry
    {
        public int Id { get; set; }
        public string ShortName { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? ExpectedAction { get; set; }
        public string? RuleCode { get; set; }
    }
}
