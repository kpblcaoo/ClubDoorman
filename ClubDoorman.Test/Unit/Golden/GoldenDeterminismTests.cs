using System.Reflection;
using NUnit.Framework;
using ClubDoorman.Services.Logging; // Access GoldenMasterRecorder type for reflection

namespace ClubDoorman.Test.Unit.Golden;

[TestFixture]
public class GoldenDeterminismTests
{
    private const string FixedTimestamp = "2025-01-01T00:00:00Z"; // must mirror baseline harness value

    [Test]
    [Category("GoldenManifest")] // picked up by workflow filter
    public void ManifestTimestamp_ShouldMatchFixedEnv_WhenFixedTimestampIsSet()
    {
        var solutionRoot = FindSolutionRoot();
        var manifestPath = Path.Combine(solutionRoot, "ClubDoorman.Baseline", "golden", "manifest.json");
        Assert.That(File.Exists(manifestPath), $"Manifest not found at {manifestPath}. Run baseline harness first.");
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(manifestPath));
        var generatedAt = doc.RootElement.GetProperty("GeneratedAtUtc").GetDateTime();
        var expected = DateTime.Parse(FixedTimestamp, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
        Assert.That(generatedAt, Is.EqualTo(expected), "Manifest GeneratedAtUtc must equal fixed timestamp for deterministic golden baselines.");
    }

    [Test]
    [Category("GoldenNorm")] // also executed in workflow
    public void StableUsernameHash_IsDeterministic_AndCaseSensitive()
    {
        var method = typeof(GoldenMasterRecorder).GetMethod("StableUsernameHash", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, "StableUsernameHash method not found via reflection (structure changed?)");

        string Hash(string s) => (string)method!.Invoke(null, new object[] { s })!;

        var samples = new[] { "Alice", "alice", "Bob_123", "пример", new string('x', 256) };
        foreach (var s in samples)
        {
            var h1 = Hash(s);
            var h2 = Hash(s);
            Assert.Multiple(() =>
            {
                Assert.That(h1, Is.EqualTo(h2), $"Hash not stable for input '{s}'");
                Assert.That(h1.Length, Is.EqualTo(8), "Hash must be first 8 hex chars of SHA256");
                Assert.That(h1, Is.EqualTo(h1.ToLowerInvariant()), "Hash must be lowercase hex");
            });
        }

        var upper = Hash("TestUser");
        var lower = Hash("testuser");
        Assert.That(upper, Is.Not.EqualTo(lower), "Username hash should be case-sensitive (SHA256 on UTF8 bytes)");
    }

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
}
