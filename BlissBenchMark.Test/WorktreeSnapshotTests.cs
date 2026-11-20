using System.Diagnostics;
using BlissBenchMark;
using NUnit.Framework;

namespace BlissBenchMark.Tests;

/// <summary>Verifies Git worktree provenance capture.</summary>
public class WorktreeSnapshotTests
{
    private readonly List<string> temporaryDirectories = new List<string>();

    /// <summary>Removes the temporary repositories created by each test.</summary>
    [TearDown]
    public void CleanUpTemporaryDirectories()
    {
        foreach (string temporaryDirectory in this.temporaryDirectories)
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }

        this.temporaryDirectories.Clear();
    }

    /// <summary>Tracked content changes alter the worktree fingerprint.</summary>
    [Test]
    public void ChangingWorktreeContentChangesDiffHash()
    {
        string repositoryPath = this.createGitRepository();
        WorktreeSnapshot cleanSnapshot = WorktreeSnapshot.Capture(repositoryPath);
        File.WriteAllText(Path.Combine(repositoryPath, "tracked.txt"), "changed");

        WorktreeSnapshot changedSnapshot = WorktreeSnapshot.Capture(repositoryPath);

        Assert.That(cleanSnapshot.IsClean, Is.True);
        Assert.That(changedSnapshot.IsClean, Is.False);
        Assert.That(changedSnapshot.DiffHash, Is.Not.EqualTo(cleanSnapshot.DiffHash));
        Assert.That(changedSnapshot.CommitSha, Is.EqualTo(cleanSnapshot.CommitSha));
    }

    /// <summary>Non-ignored untracked content is represented in the worktree fingerprint.</summary>
    [Test]
    public void UntrackedContentIsIncludedInDiffHash()
    {
        string repositoryPath = this.createGitRepository();
        WorktreeSnapshot cleanSnapshot = WorktreeSnapshot.Capture(repositoryPath);
        File.WriteAllText(Path.Combine(repositoryPath, "new.txt"), "new content");

        WorktreeSnapshot changedSnapshot = WorktreeSnapshot.Capture(repositoryPath);

        Assert.That(changedSnapshot.IsClean, Is.False);
        Assert.That(changedSnapshot.DiffHash, Is.Not.EqualTo(cleanSnapshot.DiffHash));
    }

    private string createGitRepository()
    {
        string repositoryPath = Directory.CreateTempSubdirectory("bliss-benchmark-test-").FullName;
        this.temporaryDirectories.Add(repositoryPath);
        this.runGit(repositoryPath, "init");
        this.runGit(repositoryPath, "config", "user.name", "Benchmark Test");
        this.runGit(repositoryPath, "config", "user.email", "benchmark-test@example.invalid");
        File.WriteAllText(Path.Combine(repositoryPath, "tracked.txt"), "initial");
        this.runGit(repositoryPath, "add", "tracked.txt");
        this.runGit(repositoryPath, "commit", "-m", "initial");
        return repositoryPath;
    }

    private void runGit(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Git.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.That(process.ExitCode, Is.Zero, $"{output}{error}");
    }
}
