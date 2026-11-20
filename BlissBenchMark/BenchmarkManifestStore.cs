using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlissBenchMark;

/// <summary>Persists formal benchmark runs under the ignored local artifacts directory.</summary>
public sealed class BenchmarkManifestStore
{
    private const int MAX_RUN_ID_LENGTH = 64;

    private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string artifactsDirectory;

    /// <summary>Creates a manifest store rooted at a repository's benchmark artifacts directory.</summary>
    /// <param name="repositoryRoot">The root directory of the repository.</param>
    public BenchmarkManifestStore(string repositoryRoot)
    {
        this.artifactsDirectory = Path.Combine(
            Path.GetFullPath(repositoryRoot), "BlissBenchMark", "artifacts");
    }

    /// <summary>Stores a manifest atomically and refuses to replace an existing run identifier.</summary>
    /// <param name="manifest">The formal run metadata and results to persist.</param>
    public void Save(BenchmarkRunManifest manifest)
    {
        validateRunId(manifest.RunId);
        string runDirectory = Path.Combine(this.artifactsDirectory, manifest.RunId);
        Directory.CreateDirectory(runDirectory);
        string manifestPath = Path.Combine(runDirectory, "manifest.json");
        if (File.Exists(manifestPath))
        {
            throw new InvalidOperationException($"Benchmark run '{manifest.RunId}' already exists.");
        }

        string temporaryPath = Path.Combine(runDirectory, "manifest.tmp");
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(manifest, serializerOptions));
        File.Move(temporaryPath, manifestPath);
    }

    /// <summary>Loads a previously saved formal run by its identifier.</summary>
    /// <param name="runId">The run identifier under the local artifacts directory.</param>
    public BenchmarkRunManifest Load(string runId)
    {
        validateRunId(runId);
        string manifestPath = Path.Combine(this.artifactsDirectory, runId, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Benchmark run '{runId}' was not found.", manifestPath);
        }

        BenchmarkRunManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<BenchmarkRunManifest>(
                File.ReadAllText(manifestPath), serializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Benchmark manifest '{runId}' contains invalid JSON.", exception);
        }

        if (manifest is null || manifest.SchemaVersion != BenchmarkRunManifest.CURRENT_SCHEMA_VERSION ||
            manifest.RunId != runId)
        {
            throw new InvalidDataException($"Benchmark manifest '{runId}' is invalid or has an unsupported schema.");
        }

        return manifest;
    }

    /// <summary>Returns the directory used for raw BenchmarkDotNet files for a run.</summary>
    /// <param name="runId">The run identifier under the local artifacts directory.</param>
    public string GetRawResultsDirectory(string runId)
    {
        validateRunId(runId);
        return Path.Combine(this.artifactsDirectory, runId, "raw");
    }

    /// <summary>Validates identifiers used as artifact directory names.</summary>
    /// <param name="runId">The identifier to validate.</param>
    public static void ValidateRunId(string runId) => validateRunId(runId);

    private static void validateRunId(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId) || runId.Length > MAX_RUN_ID_LENGTH ||
            !isAsciiAlphaNumeric(runId[0]) ||
            runId.Any(character => !isAsciiAlphaNumeric(character) && character != '-' && character != '_' &&
                                   character != '.'))
        {
            throw new ArgumentException(
                "Run identifiers must be 1-64 ASCII letters, digits, dots, underscores, or hyphens and start with a letter or digit.",
                nameof(runId));
        }
    }

    private static bool isAsciiAlphaNumeric(char character) =>
        character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
}
