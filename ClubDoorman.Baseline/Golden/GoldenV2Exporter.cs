using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClubDoorman.Baseline.Golden;

/// <summary>
/// Phase 3: Produce parallel simplified V2 snapshot set derived from primary baseline snapshots.
/// Does NOT re-run seeding; it transforms existing v1 outputs into a normalized minimal schema.
/// </summary>
internal static class GoldenV2Exporter
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

    private sealed class V2Snapshot
    {
        public int Schema { get; set; } = 2; // Version marker
        public string CorrelationId { get; set; } = string.Empty;
        public int Id { get; set; }
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

    public static void Export(string goldenRoot)
    {
        try
        {
            var manifestPath = Path.Combine(goldenRoot, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Console.WriteLine("[GoldenV2] manifest.json missing; skipping V2 export");
                return;
            }

            var manifestJson = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<ManifestRoot>(manifestJson, Options);
            if (manifest == null || manifest.Entries.Count == 0)
            {
                Console.WriteLine("[GoldenV2] Manifest empty; skipping");
                return;
            }
            if (!string.Equals(manifest.Variant, "baseline", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[GoldenV2] Manifest variant != baseline; skipping");
                return;
            }

            var baselineDir = Path.Combine(goldenRoot, "baseline");
            if (!Directory.Exists(baselineDir))
            {
                Console.WriteLine("[GoldenV2] Baseline directory missing; skipping");
                return;
            }

            var v2Dir = Path.Combine(goldenRoot, "baseline_v2");
            Directory.CreateDirectory(v2Dir);

            int written = 0;
            foreach (var e in manifest.Entries.OrderBy(x => x.Id))
            {
                try
                {
                    var outputPath = Path.Combine(baselineDir, e.CorrelationId + ".output.json");
                    if (!File.Exists(outputPath))
                    {
                        Console.WriteLine($"[GoldenV2] Missing output snapshot for CorrelationId={e.CorrelationId}; skipping entry");
                        continue;
                    }

                    string? action = e.ExpectedAction; // default from manifest
                    string? ruleCode = e.RuleCode;

                    // For robustness re-read source file to verify / override (source of truth)
                    using (var outDoc = JsonDocument.Parse(File.ReadAllText(outputPath)))
                    {
                        if (outDoc.RootElement.TryGetProperty("Output", out var outRoot))
                        {
                            if (outRoot.TryGetProperty("action", out var aProp)) action = aProp.GetString();
                            if (outRoot.TryGetProperty("ruleCode", out var rProp)) ruleCode = rProp.GetString();
                        }
                    }

                    var v2 = new V2Snapshot
                    {
                        CorrelationId = e.CorrelationId,
                        Id = e.Id,
                        ShortName = e.ShortName,
                        Action = action,
                        RuleCode = ruleCode
                    };

                    var v2Path = Path.Combine(v2Dir, e.CorrelationId + ".v2.json");
                    File.WriteAllText(v2Path, JsonSerializer.Serialize(v2, Options));
                    written++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GoldenV2] Failed to export entry Id={e.Id}: {ex.Message}");
                }
            }

            Console.WriteLine($"[GoldenV2] Export complete. Written {written} V2 snapshots -> {v2Dir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoldenV2] Fatal error during export: {ex}");
        }
    }
}
