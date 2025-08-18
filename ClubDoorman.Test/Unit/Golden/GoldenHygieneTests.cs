using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Golden;

/// <summary>
/// Repo hygiene guard after Phase 9 removal of legacy v1 output layer and introduction of local-only semantics files.
/// Ensures no developer accidentally commits deprecated or local-only artifacts.
/// </summary>
[TestFixture]
[Category("GoldenHygiene")]
public class GoldenHygieneTests
{
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
    public void Repository_ShouldNotContain_LegacyOrLocalGoldenArtifacts()
    {
        var root = FindSolutionRoot();

        // 1. Legacy v1 output snapshots (*.output.json) must never reappear.
        var legacyOutputs = Directory.GetFiles(root, "*.output.json", SearchOption.AllDirectories)
            .Where(p => !p.Contains("/obj/") && !p.Contains("/bin/"))
            .ToList();
        Assert.That(legacyOutputs, Is.Empty,
            () => "Found forbidden legacy v1 output snapshot(s):\n" + string.Join('\n', legacyOutputs));

        // 2. Local semantics artifacts (*.sem.json) must NOT be committed (they are transient build-time aids).
        var semFiles = Directory.GetFiles(root, "*.sem.json", SearchOption.AllDirectories)
            .Where(p => !p.Contains("/obj/") && !p.Contains("/bin/"))
            .ToList();
        Assert.That(semFiles, Is.Empty,
            () => "Found committed semantics file(s) (*.sem.json) which must remain local-only: \n" + string.Join('\n', semFiles));
    }
}
