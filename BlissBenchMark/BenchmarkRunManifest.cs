using System.Text.Json.Serialization;

namespace BlissBenchMark;

/// <summary>Machine-readable provenance and results for one formal benchmark run.</summary>
public sealed record BenchmarkRunManifest
{
    /// <summary>Current manifest format version.</summary>
    public const int CURRENT_SCHEMA_VERSION = 1;

    /// <summary>Manifest format version.</summary>
    [JsonRequired]
    public int SchemaVersion { get; init; } = CURRENT_SCHEMA_VERSION;

    /// <summary>Unique, user-supplied identifier for this run.</summary>
    public required string RunId { get; init; }

    /// <summary>UTC time when the run was recorded.</summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Git commit checked out when the benchmark started.</summary>
    public required string CommitSha { get; init; }

    /// <summary>Whether the Git worktree had no tracked or untracked changes.</summary>
    public required bool WorktreeClean { get; init; }

    /// <summary>SHA-256 fingerprint of tracked and non-ignored untracked changes.</summary>
    public required string WorktreeDiffHash { get; init; }

    /// <summary>Human-readable description of the measured workload.</summary>
    public required string Workload { get; init; }

    /// <summary>Human-readable description of work performed outside measurement.</summary>
    public required string Preparation { get; init; }

    /// <summary>BenchmarkDotNet settings and version used for this run.</summary>
    public required BenchmarkRunConfiguration Configuration { get; init; }

    /// <summary>Machine and .NET environment used for this run.</summary>
    public required BenchmarkRunEnvironment Environment { get; init; }

    /// <summary>Measured benchmark methods, actual parameters, and mean durations.</summary>
    public required IReadOnlyList<BenchmarkResult> Benchmarks { get; init; }
}

/// <summary>Reproducible BenchmarkDotNet settings for a formal run.</summary>
/// <param name="Profile">The named run profile.</param>
/// <param name="BenchmarkDotNetVersion">The BenchmarkDotNet assembly version.</param>
/// <param name="Runtime">The target .NET runtime.</param>
/// <param name="LaunchCount">The number of independent launches.</param>
/// <param name="WarmupCount">The number of warmup iterations per launch.</param>
/// <param name="IterationCount">The number of measured iterations per launch.</param>
/// <param name="Logger">The configured BenchmarkDotNet logger.</param>
/// <param name="ColumnProvider">The configured BenchmarkDotNet column provider.</param>
/// <param name="Exporter">The configured BenchmarkDotNet exporter.</param>
/// <param name="Diagnoser">The configured BenchmarkDotNet diagnoser.</param>
public sealed record BenchmarkRunConfiguration(
    string Profile,
    string BenchmarkDotNetVersion,
    string Runtime,
    int LaunchCount,
    int WarmupCount,
    int IterationCount,
    string Logger,
    string ColumnProvider,
    string Exporter,
    string Diagnoser);

/// <summary>Operating system, processor, and .NET details for a benchmark run.</summary>
/// <param name="OperatingSystem">The operating system description.</param>
/// <param name="Architecture">The process architecture.</param>
/// <param name="Cpu">The processor description.</param>
/// <param name="ProcessorCount">The number of processors available to the process.</param>
/// <param name="DotNetSdk">The active .NET SDK version.</param>
/// <param name="DotNetRuntime">The active .NET runtime description.</param>
public sealed record BenchmarkRunEnvironment(
    string OperatingSystem,
    string Architecture,
    string Cpu,
    int ProcessorCount,
    string DotNetSdk,
    string DotNetRuntime);

/// <summary>One actual BenchmarkDotNet case and its measured mean.</summary>
/// <param name="Name">The benchmark class and method name.</param>
/// <param name="Parameters">The actual benchmark parameter names and values.</param>
/// <param name="MeanNanoseconds">The measured mean duration in nanoseconds.</param>
public sealed record BenchmarkResult(
    string Name,
    IReadOnlyDictionary<string, string> Parameters,
    double MeanNanoseconds);

/// <summary>Outcome of checking and comparing two formal benchmark manifests.</summary>
/// <param name="IsComparable">Whether the manifests have compatible measurement conditions.</param>
/// <param name="Incompatibilities">The reasons comparison was rejected, if any.</param>
/// <param name="Measurements">Duration deltas for matching benchmark cases.</param>
public sealed record BenchmarkComparisonResult(
    bool IsComparable,
    IReadOnlyList<string> Incompatibilities,
    IReadOnlyList<BenchmarkMeasurementComparison> Measurements);

/// <summary>Measured duration change for one matching benchmark case.</summary>
/// <param name="Name">The benchmark class and method name.</param>
/// <param name="BaselineMeanNanoseconds">The baseline mean duration.</param>
/// <param name="CandidateMeanNanoseconds">The candidate mean duration.</param>
/// <param name="ChangePercentage">The candidate duration change relative to baseline.</param>
public sealed record BenchmarkMeasurementComparison(
    string Name,
    double BaselineMeanNanoseconds,
    double CandidateMeanNanoseconds,
    double ChangePercentage);

/// <summary>Checks whether formal benchmark runs have matching measurement conditions.</summary>
public static class BenchmarkRunComparer
{
    /// <summary>Compares two manifests only when workload, parameters, configuration, and environment match.</summary>
    /// <param name="baseline">The explicitly selected baseline manifest.</param>
    /// <param name="candidate">The explicitly selected candidate manifest.</param>
    /// <returns>Compatibility details and deltas only when the runs are comparable.</returns>
    public static BenchmarkComparisonResult Compare(
        BenchmarkRunManifest baseline,
        BenchmarkRunManifest candidate)
    {
        List<string> incompatibilities = new List<string>();
        addMismatch(baseline.Workload != candidate.Workload, "Workload", incompatibilities);
        addMismatch(baseline.Preparation != candidate.Preparation, "Preparation", incompatibilities);
        addMismatch(baseline.Configuration != candidate.Configuration, "BenchmarkDotNet configuration", incompatibilities);
        addMismatch(baseline.Environment != candidate.Environment, "Environment", incompatibilities);
        addMismatch(
            baseline.Benchmarks.Count == 0 || candidate.Benchmarks.Count == 0,
            "No measured benchmarks",
            incompatibilities);

        Dictionary<string, BenchmarkResult> baselineBenchmarks = indexBenchmarks(baseline.Benchmarks);
        Dictionary<string, BenchmarkResult> candidateBenchmarks = indexBenchmarks(candidate.Benchmarks);
        if (!baselineBenchmarks.Keys.OrderBy(key => key, StringComparer.Ordinal)
                .SequenceEqual(candidateBenchmarks.Keys.OrderBy(key => key, StringComparer.Ordinal), StringComparer.Ordinal))
        {
            incompatibilities.Add("Benchmark parameters");
        }

        if (incompatibilities.Count > 0)
        {
            return new BenchmarkComparisonResult(false, incompatibilities, Array.Empty<BenchmarkMeasurementComparison>());
        }

        List<BenchmarkMeasurementComparison> measurements = new List<BenchmarkMeasurementComparison>();
        foreach (KeyValuePair<string, BenchmarkResult> entry in baselineBenchmarks.OrderBy(
                     item => item.Key, StringComparer.Ordinal))
        {
            BenchmarkResult candidateBenchmark = candidateBenchmarks[entry.Key];
            if (!double.IsFinite(entry.Value.MeanNanoseconds) || entry.Value.MeanNanoseconds <= 0 ||
                !double.IsFinite(candidateBenchmark.MeanNanoseconds) || candidateBenchmark.MeanNanoseconds <= 0)
            {
                incompatibilities.Add($"Invalid mean duration for {entry.Value.Name}.");
                continue;
            }

            double changePercentage =
                (candidateBenchmark.MeanNanoseconds / entry.Value.MeanNanoseconds - 1) * 100;
            if (!double.IsFinite(changePercentage))
            {
                incompatibilities.Add($"Invalid duration change for {entry.Value.Name}.");
                continue;
            }

            measurements.Add(new BenchmarkMeasurementComparison(
                entry.Value.Name,
                entry.Value.MeanNanoseconds,
                candidateBenchmark.MeanNanoseconds,
                changePercentage));
        }

        return incompatibilities.Count > 0
            ? new BenchmarkComparisonResult(false, incompatibilities, Array.Empty<BenchmarkMeasurementComparison>())
            : new BenchmarkComparisonResult(true, incompatibilities, measurements);
    }

    /// <summary>Reports why a candidate has no comparable implementation without calculating a percentage.</summary>
    /// <param name="candidateRunId">The formal candidate run identifier.</param>
    /// <param name="reason">Why no equivalent baseline is available.</param>
    /// <returns>A report that explicitly omits a measured percentage.</returns>
    public static string CreateNoBaselineReport(string candidateRunId, string reason)
    {
        if (string.IsNullOrWhiteSpace(candidateRunId))
        {
            throw new ArgumentException("A candidate run identifier is required.", nameof(candidateRunId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required.", nameof(reason));
        }

        return $"NO COMPARABLE BASELINE for {candidateRunId}: {reason.Trim()}";
    }

    private static Dictionary<string, BenchmarkResult> indexBenchmarks(IReadOnlyList<BenchmarkResult> benchmarks)
    {
        Dictionary<string, BenchmarkResult> indexed = new Dictionary<string, BenchmarkResult>(StringComparer.Ordinal);
        foreach (BenchmarkResult benchmark in benchmarks)
        {
            string key = createBenchmarkKey(benchmark);
            if (!indexed.TryAdd(key, benchmark))
            {
                throw new InvalidDataException($"Duplicate benchmark case '{benchmark.Name}' with the same parameters.");
            }
        }

        return indexed;
    }

    private static string createBenchmarkKey(BenchmarkResult benchmark)
    {
        return $"{benchmark.Name}\0{string.Join("\0", benchmark.Parameters.OrderBy(
            item => item.Key, StringComparer.Ordinal).Select(item => $"{item.Key}={item.Value}"))}";
    }

    private static void addMismatch(bool mismatch, string name, ICollection<string> incompatibilities)
    {
        if (mismatch)
        {
            incompatibilities.Add(name);
        }
    }
}
