using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClubDoorman.Baseline.Golden;

/// <summary>
/// Phase 5: produce aggregate metrics over manifest + normalized snapshots for quick diff gating.
/// Output: golden/aggregates.json (Schema=5) containing counts per Action and RuleCode plus summary.
/// Future phases can add hash lists for quick change detection.
/// </summary>
internal static class GoldenAggregateBuilder
{
    private sealed record ManifestEntry(int Id, string ShortName, string CorrelationId, string? ExpectedAction, string? RuleCode);
    private sealed record ManifestRoot(int Schema, string Variant, List<ManifestEntry> Entries);

    private sealed record Aggregates
    {
        public int Schema { get; init; } = 5;
        public string Variant { get; init; } = string.Empty;
        public int Total { get; init; }
        public Dictionary<string, string> ActionCounts { get; init; } = new(); // value stored as string for stable JSON lexical ordering (we will format externally)
        public Dictionary<string, string> RuleCodeCounts { get; init; } = new();
        public string RuleCodesFingerprint { get; init; } = string.Empty; // sorted pipe-joined list of distinct codes
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Build(string goldenRoot, string variant)
    {
        try
        {
            var manifestPath = Path.Combine(goldenRoot, "manifest.json");
            if (!File.Exists(manifestPath)) return;
            var manifest = JsonSerializer.Deserialize<ManifestRoot>(File.ReadAllText(manifestPath), Options);
            if (manifest == null || manifest.Entries.Count == 0 || !string.Equals(manifest.Variant, variant, StringComparison.OrdinalIgnoreCase)) return;

            var total = manifest.Entries.Count;
            var actionCounts = manifest.Entries
                .GroupBy(e => e.ExpectedAction ?? "(null)")
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count().ToString());
            var ruleCodeCounts = manifest.Entries
                .GroupBy(e => e.RuleCode ?? "(null)")
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count().ToString());
            var fingerprint = string.Join('|', ruleCodeCounts.Keys.Where(k => k != "(null)").OrderBy(k => k));

            var aggregates = new Aggregates
            {
                Variant = variant,
                Total = total,
                ActionCounts = actionCounts,
                RuleCodeCounts = ruleCodeCounts,
                RuleCodesFingerprint = fingerprint
            };

            var outPath = Path.Combine(goldenRoot, "aggregates.json");
            File.WriteAllText(outPath, JsonSerializer.Serialize(aggregates, Options));
            Console.WriteLine($"[GoldenAgg] Wrote aggregates.json (Schema=5) -> {outPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoldenAgg] Failed: {ex.Message}");
        }
    }
}
