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
        // We allow their presence on disk during/after a local regeneration run, but they must not be tracked by git.
        var allSemFiles = Directory.GetFiles(root, "*.sem.json", SearchOption.AllDirectories)
            .Where(p => !p.Contains("/obj/") && !p.Contains("/bin/"))
            .ToList();

        // Build a hash set of tracked repository files via 'git ls-files'. If git is unavailable, fall back to previous strict behavior.
        HashSet<string> tracked = new(StringComparer.OrdinalIgnoreCase);
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "ls-files -z",
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi)!;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);
            if (proc.ExitCode == 0)
            {
                foreach (var rel in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
                {
                    // Normalise to forward slashes for comparison with discovered paths
                    var full = Path.GetFullPath(Path.Combine(root, rel));
                    tracked.Add(full);
                }
            }
        }
        catch
        {
            // ignore - will treat as non-git environment
        }

        // Consider only semantics files that are tracked. Untracked transient files are acceptable.
        var trackedSemFiles = allSemFiles.Where(f => tracked.Contains(f)).ToList();

        if (tracked.Count == 0)
        {
            // Git detection failed; retain previous strict behaviour to stay safe in CI.
            trackedSemFiles = allSemFiles;
        }

        Assert.That(trackedSemFiles, Is.Empty,
            () => "Found tracked semantics file(s) (*.sem.json) which must remain local-only: \n" + string.Join('\n', trackedSemFiles));
    }
}
