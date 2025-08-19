using System.Text.Json;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Golden;

[TestFixture]
[Category("GoldenAgg")]
public class GoldenAggregateTests
{
    [Test]
    public void Aggregates_ShouldAlignWithManifest()
    {
        var root = FindSolutionRoot();
        var goldenRoot = Path.Combine(root, "ClubDoorman.Baseline", "golden");
        var manifestPath = Path.Combine(goldenRoot, "manifest.json");
        Assume.That(File.Exists(manifestPath), "Manifest missing");
        var manifestJson = File.ReadAllText(manifestPath);
        using var mDoc = JsonDocument.Parse(manifestJson);
        var entries = mDoc.RootElement.GetProperty("Entries").EnumerateArray().Select(e => new
        {
            Action = e.TryGetProperty("ExpectedAction", out var a) ? a.GetString() ?? "(null)" : "(null)",
            RuleCode = e.TryGetProperty("RuleCode", out var r) ? r.GetString() ?? "(null)" : "(null)"
        }).ToList();

        var aggregatesPath = Path.Combine(goldenRoot, "aggregates.json");
        Assert.That(File.Exists(aggregatesPath), "aggregates.json missing (Phase 5 builder)");
        using var aDoc = JsonDocument.Parse(File.ReadAllText(aggregatesPath));
        var rootEl = aDoc.RootElement;
        Assert.That(rootEl.GetProperty("Schema").GetInt32(), Is.EqualTo(5));

        // Totals
        Assert.That(rootEl.GetProperty("Total").GetInt32(), Is.EqualTo(entries.Count), "Total mismatch");

        // Action counts parity
        var actCounts = entries.GroupBy(e => e.Action).ToDictionary(g => g.Key, g => g.Count());
        foreach (var kv in actCounts)
        {
            var jsonVal = rootEl.GetProperty("ActionCounts").GetProperty(kv.Key).GetString();
            Assert.That(int.Parse(jsonVal!), Is.EqualTo(kv.Value), $"Action count mismatch for {kv.Key}");
        }

        // RuleCode counts parity
        var rcCounts = entries.GroupBy(e => e.RuleCode).ToDictionary(g => g.Key, g => g.Count());
        foreach (var kv in rcCounts)
        {
            var jsonVal = rootEl.GetProperty("RuleCodeCounts").GetProperty(kv.Key).GetString();
            Assert.That(int.Parse(jsonVal!), Is.EqualTo(kv.Value), $"RuleCode count mismatch for {kv.Key}");
        }

        // Fingerprint parity
        var expectedFingerprint = string.Join('|', rcCounts.Keys.Where(k => k != "(null)").OrderBy(k => k));
        Assert.That(rootEl.GetProperty("RuleCodesFingerprint").GetString(), Is.EqualTo(expectedFingerprint));
    }

    private static string FindSolutionRoot()
    {
        var dir = TestContext.CurrentContext.TestDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (Directory.GetFiles(dir, "ClubDoorman.sln").Any()) return dir;
            dir = Directory.GetParent(dir)!.FullName;
        }
        throw new DirectoryNotFoundException("Cannot locate solution root");
    }
}
