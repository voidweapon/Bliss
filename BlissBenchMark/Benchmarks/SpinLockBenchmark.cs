using BenchmarkDotNet.Attributes;

namespace BlissBenchMark;

public class SpinLockBenchmark
{
    private const int N = 10000;
    private const int MaxValue = 1000;
    private int value = 0;
    private SpinLock spinLock = new SpinLock();
    
    [Benchmark]
    public void Test1()
    {
        Action action = () =>
        {
            for (int i = 0; i < N; i++)
            {
                lock (this)
                {
                    int v = 0;
                    if (++this.value > MaxValue)
                    {
                        this.value = 0;
                    }

                    v = this.value;
                    int unused = v;
                }
            }
        };
        
        Parallel.Invoke(action, action, action);
    }
    
    [Benchmark]
    public void Test2()
    {
        Action action = () =>
        {
            for (int i = 0; i < N; i++)
            {
                bool gotLock = false;
                try
                {
                    spinLock.Enter(ref gotLock);
                    
                    int v = 0;
                    if (++this.value > MaxValue)
                    {
                        this.value = 0;
                    }

                    v = this.value;
                    int unused = v;
                }
                finally
                {
                    if(gotLock)
                        spinLock.Exit();
                }
            }
        };
        
        Parallel.Invoke(action, action, action);
    }
}