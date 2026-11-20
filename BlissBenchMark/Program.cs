namespace BlissBenchMark;

internal class Program
{
    private static int Main(string[] args)
    {
        try
        {
            return BenchmarkCli.Run(args, Console.Out, findRepositoryRoot());
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or InvalidOperationException or
                System.ComponentModel.Win32Exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static string findRepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null)
        {
            string gitMetadataPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitMetadataPath) || File.Exists(gitMetadataPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string gitMetadataPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitMetadataPath) || File.Exists(gitMetadataPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the Bliss repository root.");
    }
}