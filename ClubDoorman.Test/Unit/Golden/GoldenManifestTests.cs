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

        // Expect continuous IDs 1..N
        var ids = manifest.Entries.Select(e => e.Id).OrderBy(i => i).ToList();
        var expectedIds = Enumerable.Range(1, ids.Count).ToList();
        Assert.That(ids, Is.EquivalentTo(expectedIds), "Manifest IDs must be a contiguous 1..N sequence");

        // Check snapshot files exist & match correlation ids
        foreach (var e in manifest.Entries)
        {
            var input = Path.Combine(goldenBaselineDir, e.CorrelationId + ".input.json");
            var output = Path.Combine(goldenBaselineDir, e.CorrelationId + ".output.json");
            Assert.That(File.Exists(input), $"Missing input snapshot for correlationId={e.CorrelationId} (Id={e.Id})");
            Assert.That(File.Exists(output), $"Missing output snapshot for correlationId={e.CorrelationId} (Id={e.Id})");

            using var outDoc = JsonDocument.Parse(File.ReadAllText(output));
            var outRoot = outDoc.RootElement.GetProperty("Output");
            // RuleCode presence
            Assert.That(outRoot.TryGetProperty("ruleCode", out var rcProp), $"Output missing ruleCode for correlationId={e.CorrelationId}");
            var actualRule = rcProp.GetString();
            Assert.That(actualRule, Is.Not.Null.And.Not.Empty, $"Empty ruleCode in snapshot {e.CorrelationId}");
            Assert.That(actualRule, Is.Not.EqualTo("Unknown"), $"Unknown ruleCode still present: {e.CorrelationId}");
            if (!string.IsNullOrWhiteSpace(e.RuleCode))
            {
                Assert.That(actualRule, Is.EqualTo(e.RuleCode), $"Manifest ruleCode mismatch for Id={e.Id}");
            }
            if (!string.IsNullOrWhiteSpace(e.ExpectedAction) && outRoot.TryGetProperty("action", out var actProp))
            {
                Assert.That(actProp.GetString(), Is.EqualTo(e.ExpectedAction), $"Action mismatch for Id={e.Id}");
            }
        }

        // Orphan detection: any *.output.json not in manifest
        var allOutputFiles = Directory.GetFiles(goldenBaselineDir, "*.output.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(f => f != null)
            .Select(f => f!.EndsWith(".output", StringComparison.OrdinalIgnoreCase) ? f[..^7] : f)
            .ToHashSet();
        var manifestIds = manifest.Entries.Select(e => e.CorrelationId).ToHashSet();
        var orphans = allOutputFiles.Except(manifestIds).ToList();
        Assert.That(orphans, Is.Empty, $"Orphan output snapshots not listed in manifest: {string.Join(",", orphans)}");

        // Invariant: Pass rule code only allowed for Allow action (semantic contract for baseline)
        var passMismatches = manifest.Entries
            .Where(e => string.Equals(e.RuleCode, "Pass", StringComparison.OrdinalIgnoreCase) && !string.Equals(e.ExpectedAction, "Allow", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Id)
            .ToList();
        Assert.That(passMismatches, Is.Empty, $"Entries with RuleCode=Pass must have ExpectedAction=Allow. Offenders: {string.Join(",", passMismatches)}");
    }
}
