using BenchmarkDotNet.Attributes;
namespace BlissBenchMark;

public class BlockCopyBenchmark
{
    private int pagesize = 8192; 
    private int maxRecord;
    private byte[] _src;
    private byte[] _dst;
    private Random _random;
    private int N = 10000000;
    
    [GlobalSetup]
    public void Setup()
    {
        _src = new byte[pagesize];   
        _dst = new byte[pagesize];   
        new Random(42).NextBytes(_src);   
        _random = new Random(4);
        maxRecord = pagesize / 150;
    }

    [Benchmark]
    public void ArrayCopy()
    {
        for (int i = 0; i < N; i++)
        {
            Recordsize(out var recordsize, out var readlocation, out var writelocation);

            Array.Copy(_src, readlocation, _dst, writelocation, recordsize);
        }
    }
    

    [Benchmark]
    public void BufferBlockCopy()
    {
        for (int i = 0; i < N; i++)
        {
            Recordsize(out var recordsize, out var readlocation, out var writelocation);
        
            Buffer.BlockCopy(_src, readlocation, _dst, writelocation, recordsize);
        }
    }
    
    private void Recordsize(out int recordsize, out int readlocation, out int writelocation)
    {
        // Get size we are going to copy (pretend it is as big as 
        // some fraction of the page of the pagesize)
        recordsize = _random.Next(maxRecord);
        //recordsize = maxRecord;

        readlocation = _random.Next(pagesize - recordsize);

        // Write anywhere up to the point where we would                
        // step off the end to copy it here
        writelocation = _random.Next(pagesize - recordsize);
    }
    
}