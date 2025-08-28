using System.Text.Json;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Golden;

[TestFixture]
[Category("GoldenV2")]
public class GoldenV2ExportTests
{
    private record V2Snapshot(int Schema, string CorrelationId, int Id, string ShortName, string? Action, string? RuleCode);

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

    [Test]
    public void V2Snapshots_ShouldExist_AndMatchManifest()
    {
        var root = FindSolutionRoot();
        var goldenRoot = Path.Combine(root, "ClubDoorman.Baseline", "golden");
        var manifestPath = Path.Combine(goldenRoot, "manifest.json");
        Assume.That(File.Exists(manifestPath), "Manifest missing; baseline run not executed");
        var manifestJson = File.ReadAllText(manifestPath);
        using var doc = JsonDocument.Parse(manifestJson);
        var entries = doc.RootElement.GetProperty("Entries").EnumerateArray().Select(e => new
        {
            Id = e.GetProperty("Id").GetInt32(),
            CorrelationId = e.GetProperty("CorrelationId").GetString()!,
            ShortName = e.GetProperty("ShortName").GetString()!,
            Action = e.TryGetProperty("ExpectedAction", out var a) ? a.GetString() : null,
            RuleCode = e.TryGetProperty("RuleCode", out var r) ? r.GetString() : null
        }).OrderBy(e => e.Id).ToList();

        var v2Dir = Path.Combine(goldenRoot, "baseline_v2");
        Assert.That(Directory.Exists(v2Dir), "V2 export directory missing");

        foreach (var entry in entries)
        {
            var v2Path = Path.Combine(v2Dir, entry.CorrelationId + ".v2.json");
            Assert.That(File.Exists(v2Path), $"Missing V2 snapshot for {entry.CorrelationId}");
            using var v2Doc = JsonDocument.Parse(File.ReadAllText(v2Path));
            var rootEl = v2Doc.RootElement;
            Assert.That(rootEl.GetProperty("Schema").GetInt32(), Is.EqualTo(2), "Schema marker mismatch");
            Assert.That(rootEl.GetProperty("CorrelationId").GetString(), Is.EqualTo(entry.CorrelationId));
            Assert.That(rootEl.GetProperty("Id").GetInt32(), Is.EqualTo(entry.Id));
            Assert.That(rootEl.GetProperty("ShortName").GetString(), Is.EqualTo(entry.ShortName));
            if (entry.Action != null && rootEl.TryGetProperty("Action", out var aProp))
            {
                Assert.That(aProp.GetString(), Is.EqualTo(entry.Action), $"Action mismatch for Id={entry.Id}");
            }
            if (entry.RuleCode != null && rootEl.TryGetProperty("RuleCode", out var rProp))
            {
                Assert.That(rProp.GetString(), Is.EqualTo(entry.RuleCode), $"RuleCode mismatch for Id={entry.Id}");
            }
        }

        // RuleCode coverage parity (set equality ignoring nulls)
        var manifestCodes = entries.Select(e => e.RuleCode).Where(c => !string.IsNullOrWhiteSpace(c)).ToHashSet();
        var v2Codes = Directory.GetFiles(v2Dir, "*.v2.json")
            .Select(p => JsonDocument.Parse(File.ReadAllText(p)))
            .Select(doc2 => doc2.RootElement.TryGetProperty("RuleCode", out var rc) ? rc.GetString() : null)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToHashSet();
        Assert.That(v2Codes, Is.EquivalentTo(manifestCodes), "Mismatch in distinct RuleCode sets between manifest and V2 snapshots");
    }
}
