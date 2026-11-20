using System.Diagnostics;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Running;

namespace BlissBenchMark;

/// <summary>Collects non-personal environment facts for a benchmark manifest.</summary>
internal static class BenchmarkEnvironmentCapture
{
    internal static BenchmarkRunEnvironment Capture(string repositoryRoot)
    {
        return new BenchmarkRunEnvironment(
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            getCpuDescription(),
            Environment.ProcessorCount,
            runProcess("dotnet", repositoryRoot, "--version").Trim(),
            RuntimeInformation.FrameworkDescription);
    }

    internal static string GetBenchmarkDotNetVersion() =>
        typeof(BenchmarkSwitcher).Assembly.GetName().Version?.ToString()
        ?? throw new InvalidOperationException("BenchmarkDotNet version information was unavailable.");

    private static string getCpuDescription()
    {
        if (OperatingSystem.IsMacOS())
        {
            return tryRunProcess("sysctl", "/", "-n", "machdep.cpu.brand_string")
                ?? throw new InvalidOperationException("Could not capture the processor description.");
        }

        if (OperatingSystem.IsLinux())
        {
            string cpuDescription = File.ReadLines("/proc/cpuinfo")
                .FirstOrDefault(line => line.StartsWith("model name", StringComparison.OrdinalIgnoreCase) ||
                                        line.StartsWith("Hardware", StringComparison.OrdinalIgnoreCase))?
                .Split(':', 2)
                .Last()
                .Trim() ?? throw new InvalidOperationException("Could not capture the processor description.");
            return cpuDescription;
        }

        return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
            ?? throw new InvalidOperationException("Could not capture the processor description.");
    }

    private static string runProcess(string fileName, string workingDirectory, params string[] arguments)
    {
        string? output = tryRunProcess(fileName, workingDirectory, arguments);
        return output ?? throw new InvalidOperationException($"Could not capture required environment value from {fileName}.");
    }

    private static string? tryRunProcess(string fileName, string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(fileName)
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

        using Process? process = Process.Start(startInfo);
        if (process is null)
        {
            return null;
        }

        string output = process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? output.Trim() : null;
    }
}
