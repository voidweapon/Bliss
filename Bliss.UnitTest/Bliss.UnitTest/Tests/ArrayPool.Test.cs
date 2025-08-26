using System.Buffers;

namespace Bliss.Tests;

public class ArrayPool_Test
{
    [Test]
    public void Test()
    {
        var pool = ArrayPool<byte>.Create(1024, 256);
        var buffer = pool.Rent(60);  
        pool.Return(buffer);
        Assert.Pass();
    }
}