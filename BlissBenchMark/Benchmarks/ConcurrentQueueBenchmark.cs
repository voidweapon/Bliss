using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;

namespace BlissBenchMark;

public class ConcurrentQueueBenchmark
{
    const int N = 100000;
    
    [Benchmark]
    public void Test1()
    {
        var queue = new ConcurrentQueue<int>();
        for (int i = 0; i < N; i++)
        {
            queue.Enqueue(i);
        }
        for (int i = 0; i < N; i++)
        {
            int unused = queue.Count;
        }
    }
    
    [Benchmark]
    public void Test2()
    {
        var queue = new ConcurrentQueue<int>();
        for (int i = 0; i < N; i++)
        {
            queue.Enqueue(i);
            int unused = queue.Count;
        }
    }
}