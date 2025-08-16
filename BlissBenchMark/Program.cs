using BenchmarkDotNet.Running;

namespace BlissBenchMark;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<ConcurrentQueueBenchmark>();
    }
}