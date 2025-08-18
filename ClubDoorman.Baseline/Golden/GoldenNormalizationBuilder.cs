using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClubDoorman.Baseline.Golden;

/// <summary>
/// Phase 4: Produce normalized (schema=4) snapshots focused strictly on stable semantic fields.
/// Derived from V2 (preferred) or manifest if V2 missing.
/// </summary>
internal static class GoldenNormalizationBuilder
{
    private sealed class ManifestEntry
    {
        public int Id { get; set; }
        public string ShortName { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? ExpectedAction { get; set; }
        public string? RuleCode { get; set; }
    }

    private sealed class ManifestRoot
    {
        public int Schema { get; set; }
        public string Variant { get; set; } = string.Empty;
        public List<ManifestEntry> Entries { get; set; } = new();
    }

    private sealed class NormalizedSnapshot
    {
        public int Schema { get; set; } = 4; // Phase 4 schema marker
        public int Id { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string? Action { get; set; }
        public string? RuleCode { get; set; }
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Build(string goldenRoot)
    {
        try
        {
            var manifestPath = Path.Combine(goldenRoot, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Console.WriteLine("[GoldenNorm] manifest.json missing; skipping");
                return;
            }
            var manifestJson = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<ManifestRoot>(manifestJson, Options);
            if (manifest == null || manifest.Entries.Count == 0 || !string.Equals(manifest.Variant, "baseline", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[GoldenNorm] Manifest invalid/empty or variant mismatch; skipping");
                return;
            }

            var v2Dir = Path.Combine(goldenRoot, "baseline_v2");
            var normDir = Path.Combine(goldenRoot, "baseline_norm");
            Directory.CreateDirectory(normDir);

            int written = 0;
            foreach (var e in manifest.Entries.OrderBy(x => x.Id))
            {
                try
                {
                    string? action = e.ExpectedAction;
                    string? ruleCode = e.RuleCode;

                    // Prefer V2 snapshot as source of truth if present
                    var v2Path = Path.Combine(v2Dir, e.CorrelationId + ".v2.json");
                    if (File.Exists(v2Path))
                    {
                        using var v2Doc = JsonDocument.Parse(File.ReadAllText(v2Path));
                        var root = v2Doc.RootElement;
                        if (root.TryGetProperty("Action", out var a)) action = a.GetString();
                        if (root.TryGetProperty("RuleCode", out var r)) ruleCode = r.GetString();
                    }
                    // If semantics still missing in V2, try .sem.json as last resort
                    if ((string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(ruleCode)) && File.Exists(Path.Combine(goldenRoot, "baseline", e.CorrelationId + ".sem.json")))
                    {
                        try
                        {
                            using var semDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(goldenRoot, "baseline", e.CorrelationId + ".sem.json")));
                            if (string.IsNullOrWhiteSpace(action) && semDoc.RootElement.TryGetProperty("action", out var a)) action = a.GetString();
                            if (string.IsNullOrWhiteSpace(ruleCode) && semDoc.RootElement.TryGetProperty("ruleCode", out var r)) ruleCode = r.GetString();
                        }
                        catch { /* ignore */ }
                    }

                    var norm = new NormalizedSnapshot
                    {
                        Id = e.Id,
                        CorrelationId = e.CorrelationId,
                        ShortName = e.ShortName,
                        Action = action,
                        RuleCode = ruleCode
                    };

                    var normPath = Path.Combine(normDir, e.CorrelationId + ".norm.json");
                    File.WriteAllText(normPath, JsonSerializer.Serialize(norm, Options));
                    written++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GoldenNorm] Failed Id={e.Id}: {ex.Message}");
                }
            }
            Console.WriteLine($"[GoldenNorm] Export complete. Written {written} normalized snapshots -> {normDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoldenNorm] Fatal error: {ex}");
        }
    }
}
