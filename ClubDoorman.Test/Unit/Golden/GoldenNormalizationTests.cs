using System.Text.Json;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Golden;

[TestFixture]
[Category("GoldenNorm")]
public class GoldenNormalizationTests
{
    private record Norm(int Schema, string CorrelationId, int Id, string ShortName, string? Action, string? RuleCode);

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
    public void NormalizedSnapshots_ShouldMirrorManifest_AndHaveStableSchema()
    {
        var root = FindSolutionRoot();
        var goldenRoot = Path.Combine(root, "ClubDoorman.Baseline", "golden");
        var manifestPath = Path.Combine(goldenRoot, "manifest.json");
        Assume.That(File.Exists(manifestPath), "Manifest missing; baseline not generated");
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

        var normDir = Path.Combine(goldenRoot, "baseline_norm");
        Assert.That(Directory.Exists(normDir), "Normalization directory missing");

        foreach (var entry in entries)
        {
            var path = Path.Combine(normDir, entry.CorrelationId + ".norm.json");
            Assert.That(File.Exists(path), $"Missing normalized snapshot for {entry.CorrelationId}");
            using var nDoc = JsonDocument.Parse(File.ReadAllText(path));
            var rootEl = nDoc.RootElement;
            Assert.That(rootEl.GetProperty("Schema").GetInt32(), Is.EqualTo(4), "Schema marker must be 4");
            Assert.That(rootEl.GetProperty("Id").GetInt32(), Is.EqualTo(entry.Id));
            Assert.That(rootEl.GetProperty("CorrelationId").GetString(), Is.EqualTo(entry.CorrelationId));
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

        // Distinct RuleCode parity with manifest
        var manifestCodes = entries.Select(e => e.RuleCode).Where(c => !string.IsNullOrWhiteSpace(c)).ToHashSet();
        var normCodes = Directory.GetFiles(normDir, "*.norm.json")
            .Select(p => JsonDocument.Parse(File.ReadAllText(p)))
            .Select(d => d.RootElement.TryGetProperty("RuleCode", out var rc) ? rc.GetString() : null)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToHashSet();
        Assert.That(normCodes, Is.EquivalentTo(manifestCodes), "Mismatch in distinct RuleCode sets between manifest and normalized snapshots");
    }
}
