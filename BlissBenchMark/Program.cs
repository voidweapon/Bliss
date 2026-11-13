using BenchmarkDotNet.Running;

namespace BlissBenchMark;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}