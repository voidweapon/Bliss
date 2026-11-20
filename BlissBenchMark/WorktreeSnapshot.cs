using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace BlissBenchMark;

/// <summary>Identifies the committed source and local changes present for a run.</summary>
/// <param name="CommitSha">The checked-out Git commit.</param>
/// <param name="IsClean">Whether tracked and non-ignored untracked files are unchanged.</param>
/// <param name="DiffHash">A SHA-256 fingerprint of tracked and non-ignored untracked changes.</param>
public sealed record WorktreeSnapshot(string CommitSha, bool IsClean, string DiffHash)
{
    private const int HASH_READ_BUFFER_SIZE = 81920;

    /// <summary>Captures HEAD and fingerprints tracked plus non-ignored untracked worktree content.</summary>
    /// <param name="repositoryDirectory">The root directory of the Git repository.</param>
    /// <returns>The commit, clean state, and content fingerprint.</returns>
    public static WorktreeSnapshot Capture(string repositoryDirectory)
    {
        string repositoryRoot = Path.GetFullPath(repositoryDirectory);
        string commitSha = runGitText(repositoryRoot, "rev-parse", "HEAD").Trim();
        byte[] status = runGitBytes(repositoryRoot, "status", "--porcelain=v1", "--untracked-files=all", "-z");
        string[] untrackedFiles = runGitText(repositoryRoot, "ls-files", "--others", "--exclude-standard", "-z")
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        appendGitDiff(repositoryRoot, hash);
        foreach (string relativePath in untrackedFiles)
        {
            appendFile(hash, repositoryRoot, relativePath);
        }

        return new WorktreeSnapshot(
            commitSha,
            status.Length == 0,
            Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    private static void appendGitDiff(string repositoryRoot, IncrementalHash hash)
    {
        using Process process = startGit(repositoryRoot, "diff", "HEAD", "--binary");
        Stream output = process.StandardOutput.BaseStream;
        byte[] buffer = new byte[HASH_READ_BUFFER_SIZE];
        int bytesRead;
        while ((bytesRead = output.Read(buffer, 0, buffer.Length)) > 0)
        {
            hash.AppendData(buffer, 0, bytesRead);
        }

        finishGit(process);
    }

    private static void appendFile(IncrementalHash hash, string repositoryRoot, string relativePath)
    {
        byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.Replace(Path.DirectorySeparatorChar, '/'));
        Span<byte> lengthBuffer = stackalloc byte[sizeof(int)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer, pathBytes.Length);
        hash.AppendData(lengthBuffer);
        hash.AppendData(pathBytes);

        string fullPath = Path.Combine(repositoryRoot, relativePath);
        FileSystemInfo fileSystemInfo = Directory.Exists(fullPath)
            ? new DirectoryInfo(fullPath)
            : new FileInfo(fullPath);
        if (fileSystemInfo.LinkTarget is string linkTarget)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(linkTarget));
            return;
        }

        using FileStream file = File.OpenRead(fullPath);
        byte[] buffer = new byte[HASH_READ_BUFFER_SIZE];
        int bytesRead;
        while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
        {
            hash.AppendData(buffer, 0, bytesRead);
        }
    }

    private static string runGitText(string repositoryRoot, params string[] arguments)
    {
        using Process process = startGit(repositoryRoot, arguments);
        string output = process.StandardOutput.ReadToEnd();
        finishGit(process);
        return output;
    }

    private static byte[] runGitBytes(string repositoryRoot, params string[] arguments)
    {
        using Process process = startGit(repositoryRoot, arguments);
        using MemoryStream output = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(output);
        finishGit(process);
        return output.ToArray();
    }

    private static Process startGit(string repositoryRoot, params string[] arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Git.");
    }

    private static void finishGit(Process process)
    {
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git failed: {error.Trim()}");
        }
    }
}
