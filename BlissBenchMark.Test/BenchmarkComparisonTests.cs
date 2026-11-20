using BlissBenchMark;
using NUnit.Framework;

namespace BlissBenchMark.Tests;

/// <summary>Verifies formal benchmark comparison rules.</summary>
public class BenchmarkComparisonTests
{
    /// <summary>Matching formal runs produce a measured duration delta.</summary>
    [Test]
    public void MatchingRunsProduceMeasuredDelta()
    {
        BenchmarkRunManifest baseline = this.createManifest("baseline", 100);
        BenchmarkRunManifest candidate = this.createManifest("candidate", 80);

        BenchmarkComparisonResult result = BenchmarkRunComparer.Compare(baseline, candidate);

        Assert.That(result.IsComparable, Is.True);
        Assert.That(result.Measurements, Has.Count.EqualTo(1));
        Assert.That(result.Measurements[0].ChangePercentage, Is.EqualTo(-20).Within(0.001));
    }

    /// <summary>A workload mismatch prevents the calculation of a duration delta.</summary>
    [Test]
    public void WorkloadMismatchDoesNotProduceMeasuredDelta()
    {
        BenchmarkRunManifest baseline = this.createManifest("baseline", 100);
        BenchmarkRunManifest candidate = this.createManifest("candidate", 80) with
        {
            Workload = "different workload",
        };

        BenchmarkComparisonResult result = BenchmarkRunComparer.Compare(baseline, candidate);

        Assert.That(result.IsComparable, Is.False);
        Assert.That(result.Incompatibilities, Does.Contain("Workload"));
        Assert.That(result.Measurements, Is.Empty);
    }

    /// <summary>A preparation mismatch prevents the calculation of a duration delta.</summary>
    [Test]
    public void PreparationMismatchDoesNotProduceMeasuredDelta()
    {
        BenchmarkRunManifest baseline = this.createManifest("baseline", 100);
        BenchmarkRunManifest candidate = this.createManifest("candidate", 80) with
        {
            Preparation = "different preparation",
        };

        BenchmarkComparisonResult result = BenchmarkRunComparer.Compare(baseline, candidate);

        Assert.That(result.IsComparable, Is.False);
        Assert.That(result.Incompatibilities, Does.Contain("Preparation"));
        Assert.That(result.Measurements, Is.Empty);
    }

    /// <summary>A BenchmarkDotNet configuration mismatch prevents a measured comparison.</summary>
    [Test]
    public void ConfigurationMismatchDoesNotProduceMeasuredDelta()
    {
        BenchmarkRunManifest baseline = this.createManifest("baseline", 100);
        BenchmarkRunManifest candidate = this.createManifest("candidate", 80) with
        {
            Configuration = this.createConfiguration(launchCount: 2),
        };

        BenchmarkComparisonResult result = BenchmarkRunComparer.Compare(baseline, candidate);

        Assert.That(result.IsComparable, Is.False);
        Assert.That(result.Incompatibilities, Does.Contain("BenchmarkDotNet configuration"));
        Assert.That(result.Measurements, Is.Empty);
    }

    /// <summary>An environment mismatch prevents a measured comparison.</summary>
    [Test]
    public void EnvironmentMismatchDoesNotProduceMeasuredDelta()
    {
        BenchmarkRunManifest baseline = this.createManifest("baseline", 100);
        BenchmarkRunManifest candidate = this.createManifest("candidate", 80) with
        {
            Environment = new BenchmarkRunEnvironment("Linux", "Arm64", "Apple M1 Pro", 10, "8.0.413", ".NET 8.0.19"),
        };

        BenchmarkComparisonResult result = BenchmarkRunComparer.Compare(baseline, candidate);

        Assert.That(result.IsComparable, Is.False);
        Assert.That(result.Incompatibilities, Does.Contain("Environment"));
        Assert.That(result.Measurements, Is.Empty);
    }

    /// <summary>Invalid or empty measurement data never produces a percentage.</summary>
    [Test]
    public void InvalidMeasurementsDoNotProduceMeasuredDelta()
    {
        BenchmarkRunManifest baseline = this.createManifest("baseline", double.NaN);
        BenchmarkRunManifest candidate = this.createManifest("candidate", 80);

        BenchmarkComparisonResult result = BenchmarkRunComparer.Compare(baseline, candidate);

        Assert.That(result.IsComparable, Is.False);
        Assert.That(result.Incompatibilities, Does.Contain("Invalid mean duration for TimerUpdate."));
        Assert.That(result.Measurements, Is.Empty);
    }

    /// <summary>Different actual parameter values make two benchmark cases incomparable.</summary>
    [Test]
    public void DifferentParameterSetsAreIncompatible()
    {
        BenchmarkRunManifest baseline = this.createManifest("baseline", 100);
        BenchmarkRunManifest candidate = this.createManifest("candidate", 80) with
        {
            Benchmarks = new[]
            {
                new BenchmarkResult(
                    "TimerUpdate",
                    new Dictionary<string, string> { ["TimerCount"] = "10000" },
                    80),
            },
        };

        BenchmarkComparisonResult result = BenchmarkRunComparer.Compare(baseline, candidate);

        Assert.That(result.IsComparable, Is.False);
        Assert.That(result.Incompatibilities, Does.Contain("Benchmark parameters"));
        Assert.That(result.Measurements, Is.Empty);
    }

    /// <summary>An explicit no-baseline report does not imply a measured percentage.</summary>
    [Test]
    public void ExplicitNoBaselineReportDoesNotContainPercentage()
    {
        string report = BenchmarkRunComparer.CreateNoBaselineReport("candidate", "No equivalent implementation exists.");

        Assert.That(report, Does.Contain("NO COMPARABLE BASELINE"));
        Assert.That(report, Does.Contain("No equivalent implementation exists."));
        Assert.That(report, Does.Not.Contain("%"));
    }

    private BenchmarkRunManifest createManifest(string runId, double meanNanoseconds)
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
            Configuration = this.createConfiguration(launchCount: 3),
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

    private BenchmarkRunConfiguration createConfiguration(int launchCount)
    {
        return new BenchmarkRunConfiguration(
            "Formal",
            "0.15.2",
            "net8.0",
            launchCount,
            5,
            10,
            "ConsoleLogger.Default",
            "DefaultColumnProviders.Instance",
            "JsonExporter.Full",
            "MemoryDiagnoser.Default");
    }
}
