using System.Globalization;
using System.Text.Json;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BlissBenchMark;

/// <summary>Command-line interface for exploratory and formal benchmark workflows.</summary>
public static class BenchmarkCli
{
    private const int FORMAL_LAUNCH_COUNT = 3;
    private const int FORMAL_WARMUP_COUNT = 5;
    private const int FORMAL_ITERATION_COUNT = 10;
    private const string FORMAL_PROFILE = "Formal";
    private const string FORMAL_RUNTIME = "net8.0";
    private const string FORMAL_LOGGER = "ConsoleLogger.Default";
    private const string FORMAL_COLUMN_PROVIDER = "DefaultColumnProviders.Instance";
    private const string FORMAL_EXPORTER = "JsonExporter.Full";
    private const string FORMAL_DIAGNOSER = "MemoryDiagnoser.Default";
#if DEBUG
    private static readonly bool isReleaseBuild = false;
#else
    private static readonly bool isReleaseBuild = true;
#endif

    /// <summary>Executes a benchmark command from the repository root.</summary>
    /// <param name="args">The command and its arguments.</param>
    /// <param name="output">The writer for user-facing results.</param>
    /// <param name="repositoryRoot">The root directory of the repository.</param>
    /// <returns>Zero for success and a nonzero exit code for command failure.</returns>
    public static int Run(string[] args, TextWriter output, string repositoryRoot)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            writeUsage(output);
            return 0;
        }

        return args[0] switch
        {
            "explore" => runExploratory(args[1..], output, repositoryRoot),
            "run" => runFormal(args[1..], output, repositoryRoot),
            "compare" => compareRuns(args[1..], output, repositoryRoot),
            "no-baseline" => reportNoBaseline(args[1..], output, repositoryRoot),
            _ => writeInvalidCommand(output, args[0]),
        };
    }

    private static int runExploratory(string[] benchmarkArguments, TextWriter output, string repositoryRoot)
    {
        output.WriteLine("EXPLORATORY RUN: results are not suitable for formal improvement or regression claims.");
        IConfig config = createConfig(Job.ShortRun.WithId("Exploratory"), null);
        Summary[] summaries = runBenchmarks(benchmarkArguments, config, repositoryRoot);
        if (hasIncompleteResults(summaries))
        {
            output.WriteLine("BenchmarkDotNet did not complete every selected exploratory benchmark successfully.");
            return 1;
        }

        return 0;
    }

    private static int runFormal(string[] args, TextWriter output, string repositoryRoot)
    {
        FormalRunOptions options = parseFormalRunOptions(args);
        if (!containsOnlyFilter(options.BenchmarkArguments))
        {
            throw new ArgumentException(
                "Formal runs require a BenchmarkDotNet --filter and do not accept arguments that could override the fixed configuration.");
        }

        if (!isReleaseBuild)
        {
            throw new InvalidOperationException("Formal benchmark runs must be launched from a Release build.");
        }

        BenchmarkManifestStore store = new BenchmarkManifestStore(repositoryRoot);
        string rawResultsDirectory = store.GetRawResultsDirectory(options.RunId);
        string runDirectory = Directory.GetParent(rawResultsDirectory)?.FullName
            ?? throw new InvalidOperationException("The benchmark artifact directory was invalid.");
        if (Directory.Exists(runDirectory))
        {
            throw new InvalidOperationException($"Benchmark run '{options.RunId}' already exists.");
        }

        WorktreeSnapshot worktreeSnapshot = WorktreeSnapshot.Capture(repositoryRoot);
        BenchmarkRunEnvironment environment = BenchmarkEnvironmentCapture.Capture(repositoryRoot);
        string benchmarkDotNetVersion = BenchmarkEnvironmentCapture.GetBenchmarkDotNetVersion();
        Directory.CreateDirectory(rawResultsDirectory);

        Job formalJob = Job.Default
            .WithId(FORMAL_PROFILE)
            .WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
            .WithLaunchCount(FORMAL_LAUNCH_COUNT)
            .WithWarmupCount(FORMAL_WARMUP_COUNT)
            .WithIterationCount(FORMAL_ITERATION_COUNT);
        IConfig config = createConfig(formalJob, rawResultsDirectory);
        Summary[] summaries = runBenchmarks(options.BenchmarkArguments, config, repositoryRoot);
        if (hasIncompleteResults(summaries))
        {
            throw new InvalidOperationException("BenchmarkDotNet did not complete every selected formal benchmark successfully.");
        }

        List<BenchmarkResult> benchmarks = summaries
            .SelectMany(summary => summary.Reports)
            .Select(createBenchmarkResult)
            .OrderBy(result => result.Name, StringComparer.Ordinal)
            .ThenBy(result => string.Join(";", result.Parameters.OrderBy(item => item.Key).Select(
                item => $"{item.Key}={item.Value}")), StringComparer.Ordinal)
            .ToList();
        if (benchmarks.Count == 0)
        {
            throw new InvalidOperationException("BenchmarkDotNet produced no measured benchmark results.");
        }

        BenchmarkRunManifest manifest = new BenchmarkRunManifest
        {
            RunId = options.RunId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CommitSha = worktreeSnapshot.CommitSha,
            WorktreeClean = worktreeSnapshot.IsClean,
            WorktreeDiffHash = worktreeSnapshot.DiffHash,
            Workload = options.Workload,
            Preparation = options.Preparation,
            Configuration = new BenchmarkRunConfiguration(
                FORMAL_PROFILE,
                benchmarkDotNetVersion,
                FORMAL_RUNTIME,
                FORMAL_LAUNCH_COUNT,
                FORMAL_WARMUP_COUNT,
                FORMAL_ITERATION_COUNT,
                FORMAL_LOGGER,
                FORMAL_COLUMN_PROVIDER,
                FORMAL_EXPORTER,
                FORMAL_DIAGNOSER),
            Environment = environment,
            Benchmarks = benchmarks,
        };
        store.Save(manifest);
        output.WriteLine($"Formal run '{manifest.RunId}' recorded in BlissBenchMark/artifacts/{manifest.RunId}/manifest.json.");
        output.WriteLine($"Raw BenchmarkDotNet files are in BlissBenchMark/artifacts/{manifest.RunId}/raw/.");
        return 0;
    }

    private static BenchmarkResult createBenchmarkResult(BenchmarkReport report)
    {
        double meanNanoseconds = report.ResultStatistics?.Mean
            ?? throw new InvalidDataException($"No mean duration was reported for {report.BenchmarkCase.DisplayInfo}.");
        string benchmarkName =
            $"{report.BenchmarkCase.Descriptor.Type.Name}.{report.BenchmarkCase.Descriptor.WorkloadMethod.Name}";
        IReadOnlyDictionary<string, string> parameters = report.BenchmarkCase.Parameters.Items.ToDictionary(
            parameter => parameter.Name,
            parameter => formatParameterValue(parameter.Value),
            StringComparer.Ordinal);
        return new BenchmarkResult(benchmarkName, parameters, meanNanoseconds);
    }

    private static string formatParameterValue(object? value)
    {
        if (value is null)
        {
            return "?";
        }

        string typeName = value.GetType().FullName ?? value.GetType().Name;
        string formattedValue = value is Array array
            ? JsonSerializer.Serialize(array, array.GetType())
            : value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty
                : value.ToString() ?? string.Empty;
        return $"{typeName}:{formattedValue}";
    }

    private static int compareRuns(string[] args, TextWriter output, string repositoryRoot)
    {
        Dictionary<string, string> options = parseNamedOptions(args, "--baseline", "--candidate");
        BenchmarkManifestStore store = new BenchmarkManifestStore(repositoryRoot);
        BenchmarkRunManifest baseline = store.Load(options["--baseline"]);
        BenchmarkRunManifest candidate = store.Load(options["--candidate"]);
        BenchmarkComparisonResult comparison = BenchmarkRunComparer.Compare(baseline, candidate);
        if (!comparison.IsComparable)
        {
            output.WriteLine("INCOMPARABLE: no duration percentage was calculated.");
            foreach (string incompatibility in comparison.Incompatibilities)
            {
                output.WriteLine($"- {incompatibility}");
            }

            return 2;
        }

        output.WriteLine($"Measured change: {baseline.RunId} -> {candidate.RunId}");
        foreach (BenchmarkMeasurementComparison measurement in comparison.Measurements)
        {
            output.WriteLine(
                $"{measurement.Name}: {measurement.BaselineMeanNanoseconds:F2} ns -> " +
                $"{measurement.CandidateMeanNanoseconds:F2} ns ({measurement.ChangePercentage:+0.00;-0.00;0.00}%)");
        }

        return 0;
    }

    private static int reportNoBaseline(string[] args, TextWriter output, string repositoryRoot)
    {
        Dictionary<string, string> options = parseNamedOptions(args, "--candidate", "--reason");
        BenchmarkRunManifest candidate = new BenchmarkManifestStore(repositoryRoot).Load(options["--candidate"]);
        string report = BenchmarkRunComparer.CreateNoBaselineReport(candidate.RunId, options["--reason"]);
        output.WriteLine(report);
        return 0;
    }

    private static IConfig createConfig(Job job, string? artifactsPath)
    {
        ManualConfig config = ManualConfig.CreateEmpty()
            .AddJob(job)
            .AddLogger(ConsoleLogger.Default)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddExporter(JsonExporter.Full)
            .AddDiagnoser(MemoryDiagnoser.Default);
        return artifactsPath is null ? config : config.WithArtifactsPath(artifactsPath);
    }

    private static Summary[] runBenchmarks(
        string[] benchmarkArguments,
        IConfig config,
        string repositoryRoot)
    {
        string previousDirectory = Environment.CurrentDirectory;
        try
        {
            Directory.SetCurrentDirectory(Path.Combine(repositoryRoot, "BlissBenchMark"));
            return BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(benchmarkArguments, config)
                .ToArray();
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
        }
    }

    private static bool hasIncompleteResults(IReadOnlyList<Summary> summaries) =>
        summaries.Count == 0 ||
        summaries.Any(summary =>
            summary.HasCriticalValidationErrors ||
            !summary.Reports.Any() ||
            summary.Reports.Any(report => report.ResultStatistics is null));

    private static FormalRunOptions parseFormalRunOptions(string[] args)
    {
        int separator = Array.IndexOf(args, "--");
        if (separator < 0)
        {
            throw new ArgumentException("Formal run options must be followed by '--' and BenchmarkDotNet arguments.");
        }

        Dictionary<string, string> options = parseNamedOptions(args[..separator], "--id", "--workload", "--preparation");
        BenchmarkManifestStore.ValidateRunId(options["--id"]);
        if (string.IsNullOrWhiteSpace(options["--workload"]) ||
            string.IsNullOrWhiteSpace(options["--preparation"]))
        {
            throw new ArgumentException("Formal runs require non-empty workload and preparation descriptions.");
        }

        string[] benchmarkArguments = args[(separator + 1)..];
        if (benchmarkArguments.Length == 0)
        {
            throw new ArgumentException("Formal runs require BenchmarkDotNet arguments after '--'.");
        }

        return new FormalRunOptions(
            options["--id"],
            options["--workload"].Trim(),
            options["--preparation"].Trim(),
            benchmarkArguments);
    }

    private static Dictionary<string, string> parseNamedOptions(string[] args, params string[] requiredNames)
    {
        HashSet<string> required = new HashSet<string>(requiredNames, StringComparer.Ordinal);
        Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index++)
        {
            string name = args[index];
            if (!required.Contains(name) || index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Expected option and value for {name}.");
            }

            if (!options.TryAdd(name, args[++index]))
            {
                throw new ArgumentException($"Option {name} was specified more than once.");
            }
        }

        string[] missing = required.Except(options.Keys, StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
        {
            throw new ArgumentException($"Missing required option(s): {string.Join(", ", missing)}.");
        }

        return options;
    }

    private static bool containsOnlyFilter(IReadOnlyList<string> arguments)
    {
        bool foundFilter = false;
        for (int index = 0; index < arguments.Count; index++)
        {
            string argument = arguments[index];
            if (argument == "--filter" && index + 1 < arguments.Count &&
                !arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                if (foundFilter)
                {
                    return false;
                }

                foundFilter = true;
                index++;
            }
            else if (argument.StartsWith("--filter=", StringComparison.Ordinal) &&
                     argument.Length > "--filter=".Length && !foundFilter)
            {
                foundFilter = true;
            }
            else
            {
                return false;
            }
        }

        return foundFilter;
    }

    private static int writeInvalidCommand(TextWriter output, string command)
    {
        output.WriteLine($"Unknown benchmark command '{command}'.");
        writeUsage(output);
        return 2;
    }

    private static void writeUsage(TextWriter output)
    {
        output.WriteLine("BlissBenchMark commands:");
        output.WriteLine("  explore <BenchmarkDotNet arguments>");
        output.WriteLine("  run --id <id> --workload <description> --preparation <description> -- <BenchmarkDotNet arguments>");
        output.WriteLine("  compare --baseline <run-id> --candidate <run-id>");
        output.WriteLine("  no-baseline --candidate <run-id> --reason <description>");
    }

    private sealed record FormalRunOptions(
        string RunId,
        string Workload,
        string Preparation,
        string[] BenchmarkArguments);
}
