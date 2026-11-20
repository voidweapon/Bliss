using BlissBenchMark;
using NUnit.Framework;

namespace BlissBenchMark.Tests;

/// <summary>Verifies manifest persistence and formal benchmark CLI boundaries.</summary>
public class BenchmarkManifestStoreTests
{
    private string repositoryRoot = string.Empty;
    private BenchmarkManifestStore store = null!;

    /// <summary>Creates an isolated artifact directory for the test.</summary>
    [SetUp]
    public void SetUp()
    {
        this.repositoryRoot = Directory.CreateTempSubdirectory("bliss-benchmark-store-test-").FullName;
        this.store = new BenchmarkManifestStore(this.repositoryRoot);
    }

    /// <summary>Removes the temporary artifact directory created by the test.</summary>
    [TearDown]
    public void CleanUp()
    {
        Directory.Delete(this.repositoryRoot, recursive: true);
    }

    /// <summary>Manifest round trips preserve results without storing local paths or user names.</summary>
    [Test]
    public void SavedManifestCanBeLoadedWithoutPersistingLocalPathsOrUserName()
    {
        BenchmarkRunManifest expectedManifest = this.createManifest("formal-run");
        this.store.Save(expectedManifest);

        BenchmarkRunManifest actualManifest = this.store.Load(expectedManifest.RunId);
        string manifestJson = File.ReadAllText(
            Path.Combine(this.repositoryRoot, "BlissBenchMark", "artifacts", expectedManifest.RunId, "manifest.json"));

        Assert.That(actualManifest.RunId, Is.EqualTo(expectedManifest.RunId));
        Assert.That(actualManifest.Configuration, Is.EqualTo(expectedManifest.Configuration));
        Assert.That(actualManifest.Environment, Is.EqualTo(expectedManifest.Environment));
        Assert.That(actualManifest.Benchmarks[0].Name, Is.EqualTo(expectedManifest.Benchmarks[0].Name));
        Assert.That(
            actualManifest.Benchmarks[0].Parameters["TimerCount"],
            Is.EqualTo(expectedManifest.Benchmarks[0].Parameters["TimerCount"]));
        Assert.That(manifestJson, Does.Not.Contain(this.repositoryRoot));
        Assert.That(manifestJson, Does.Not.Contain(Environment.UserName));
    }

    /// <summary>Run identifiers cannot escape the local artifact directory.</summary>
    [Test]
    public void RunIdentifiersCannotEscapeArtifactDirectory()
    {
        Assert.Throws<ArgumentException>(() => this.store.GetRawResultsDirectory("../outside"));
    }

    /// <summary>Invalid manifest JSON is reported as invalid local data.</summary>
    [Test]
    public void InvalidManifestJsonIsRejected()
    {
        string manifestPath = Path.Combine(
            this.repositoryRoot, "BlissBenchMark", "artifacts", "invalid-run", "manifest.json");
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        File.WriteAllText(manifestPath, "{");

        Assert.Throws<InvalidDataException>(() => this.store.Load("invalid-run"));
    }

    /// <summary>Formal commands cannot override the fixed BenchmarkDotNet configuration.</summary>
    [Test]
    public void FormalRunRejectsArgumentsThatCanOverrideItsFixedConfiguration()
    {
        string[] arguments =
        {
            "run",
            "--id",
            "formal-run",
            "--workload",
            "timer workload",
            "--preparation",
            "prepare outside measured method",
            "--",
            "--filter",
            "*Timer*",
            "--launchCount",
            "1",
        };

        Assert.Throws<ArgumentException>(() => BenchmarkCli.Run(arguments, TextWriter.Null, this.repositoryRoot));
    }

    /// <summary>The compare command reports a duration change for explicitly selected compatible runs.</summary>
    [Test]
    public void CompareCommandUsesOnlyExplicitlySelectedCompatibleRuns()
    {
        this.store.Save(this.createManifest("baseline", 100));
        this.store.Save(this.createManifest("candidate", 80));
        StringWriter output = new StringWriter();

        int exitCode = BenchmarkCli.Run(
            new[] { "compare", "--baseline", "baseline", "--candidate", "candidate" },
            output,
            this.repositoryRoot);

        Assert.That(exitCode, Is.Zero);
        Assert.That(output.ToString(), Does.Contain("-20.00%"));
    }

    /// <summary>The no-baseline command reports the reason without a duration percentage.</summary>
    [Test]
    public void NoBaselineCommandDoesNotReportPercentage()
    {
        this.store.Save(this.createManifest("candidate", 80));
        StringWriter output = new StringWriter();

        int exitCode = BenchmarkCli.Run(
            new[] { "no-baseline", "--candidate", "candidate", "--reason", "No equivalent implementation exists" },
            output,
            this.repositoryRoot);

        Assert.That(exitCode, Is.Zero);
        Assert.That(output.ToString(), Does.Contain("NO COMPARABLE BASELINE"));
        Assert.That(output.ToString(), Does.Not.Contain("%"));
    }

    private BenchmarkRunManifest createManifest(string runId, double meanNanoseconds = 100)
    {
        return new BenchmarkRunManifest
        {
            RunId = runId,
            CreatedAtUtc = DateTimeOffset.Parse("2026-11-20T00:00:00Z"),
            CommitSha = "0123456789abcdef",
            WorktreeClean = true,
            WorktreeDiffHash = "clean",
            Workload = "register 5000 timers with 100 buckets",
            Preparation = "prepare timers before iteration",
            Configuration = new BenchmarkRunConfiguration(
                "Formal",
                "0.15.2",
                "net8.0",
                3,
                5,
                10,
                "ConsoleLogger.Default",
                "DefaultColumnProviders.Instance",
                "JsonExporter.Full",
                "MemoryDiagnoser.Default"),
            Environment = new BenchmarkRunEnvironment("macOS", "Arm64", "Apple M1 Pro", 10, "8.0.413", ".NET 8.0.19"),
            Benchmarks = new[]
            {
                new BenchmarkResult(
                    "TimerUpdate",
                    new Dictionary<string, string> { ["TimerCount"] = "5000" },
                    meanNanoseconds),
            },
        };
    }
}
