using System.Text.Json;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Golden;

[TestFixture]
[Category("GoldenManifest")]
public class GoldenManifestTests
{
    private record ManifestRoot(int Schema, DateTime GeneratedAtUtc, string Variant, List<Entry> Entries);
    private record Entry(int Id, string ShortName, string CorrelationId, string? ExpectedAction, string? RuleCode);

    private static string FindSolutionRoot()
    {
        var dir = TestContext.CurrentContext.TestDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (Directory.GetFiles(dir, "ClubDoorman.sln").Any()) return dir;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new DirectoryNotFoundException("Cannot locate solution root (ClubDoorman.sln)");
    }

    private static ManifestRoot LoadManifest()
    {
        var root = FindSolutionRoot();
        var manifestPath = Path.Combine(root, "ClubDoorman.Baseline", "golden", "manifest.json");
        Assert.That(File.Exists(manifestPath), $"Manifest file not found: {manifestPath}");
        var json = File.ReadAllText(manifestPath);
        var doc = JsonDocument.Parse(json);
        var entries = new List<Entry>();
        foreach (var e in doc.RootElement.GetProperty("Entries").EnumerateArray())
        {
            entries.Add(new Entry(
                e.GetProperty("Id").GetInt32(),
                e.GetProperty("ShortName").GetString()!,
                e.GetProperty("CorrelationId").GetString()!,
                e.TryGetProperty("ExpectedAction", out var act) ? act.GetString() : null,
                e.TryGetProperty("RuleCode", out var rc) ? rc.GetString() : null
            ));
        }
        return new ManifestRoot(
            doc.RootElement.GetProperty("Schema").GetInt32(),
            doc.RootElement.GetProperty("GeneratedAtUtc").GetDateTime(),
            doc.RootElement.GetProperty("Variant").GetString()!,
            entries
        );
    }

    [Test]
    public void Manifest_ShouldCoverAllSnapshots_AndHaveNoUnknownRuleCodes()
    {
        var root = FindSolutionRoot();
    var goldenBaselineDir = Path.Combine(root, "ClubDoorman.Baseline", "golden", "baseline");
    Assert.That(Directory.Exists(goldenBaselineDir), "Baseline golden directory missing");

        var manifest = LoadManifest();
        Assert.That(manifest.Schema, Is.EqualTo(1), "Schema mismatch");
        Assert.That(manifest.Variant, Is.EqualTo("baseline"), "Variant mismatch");

    // IDs no longer required to be contiguous: we intentionally preserve historical IDs to avoid churn.
    var ids = manifest.Entries.Select(e => e.Id).OrderBy(i => i).ToList();
    var distinctCount = ids.Distinct().Count();
    Assert.That(distinctCount, Is.EqualTo(ids.Count), "Duplicate manifest Ids detected");
    // Allowed removed (gap) IDs must be explicitly whitelisted with rationale (aggregation / pruning)
    var allowedRemoved = new HashSet<int> { 8, 9, 17 }; // Phase 7 aggregation removed redundant emoji scenarios (8,9) and boundary-over case (17)
    var maxId = ids.Max();
    var missing = Enumerable.Range(1, maxId).Where(i => !ids.Contains(i)).ToList();
    var unexpectedMissing = missing.Where(m => !allowedRemoved.Contains(m)).ToList();
    Assert.That(unexpectedMissing, Is.Empty, $"Unexpected missing manifest IDs (not whitelisted gaps): {string.Join(',', unexpectedMissing)}");

        // Check only input snapshots exist (V1 output layer removed)
        foreach (var e in manifest.Entries)
        {
            var input = Path.Combine(goldenBaselineDir, e.CorrelationId + ".input.json");
            Assert.That(File.Exists(input), $"Missing input snapshot for correlationId={e.CorrelationId} (Id={e.Id})");
        }

    // Semantic invariants re-enabled after Phase 9 semantics layer introduction (.sem.json used during build):
    // 1. ExpectedAction should be non-null/non-empty
    // 2. RuleCode should be non-null/non-empty and not 'Unknown'
    var missingAction = manifest.Entries.Where(e => string.IsNullOrWhiteSpace(e.ExpectedAction)).Select(e => e.Id).ToList();
    var missingRule = manifest.Entries.Where(e => string.IsNullOrWhiteSpace(e.RuleCode)).Select(e => e.Id).ToList();
    Assert.That(missingAction, Is.Empty, $"Entries missing ExpectedAction (Phase 9 regression?): {string.Join(',', missingAction)}");
    Assert.That(missingRule, Is.Empty, $"Entries missing RuleCode (Phase 9 regression?): {string.Join(',', missingRule)}");
    var unknownRule = manifest.Entries.Where(e => string.Equals(e.RuleCode, "Unknown", StringComparison.OrdinalIgnoreCase)).Select(e => e.Id).ToList();
    Assert.That(unknownRule, Is.Empty, $"Entries with 'Unknown' RuleCode: {string.Join(',', unknownRule)}");
    }
}
