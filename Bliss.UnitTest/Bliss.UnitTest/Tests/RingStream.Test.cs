using Bliss.IO;

namespace Bliss.Tests;

public class RingStream_Test
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ReadWriteBuffer()
    {
        var ringStream = new RingStream();
        int N = 10000;
        var buffer = new byte[N];
        (new Random(3)).NextBytes(buffer);
        
        ringStream.Write(buffer, 0, buffer.Length);
        
        var result = new byte[N];
        ringStream.Read(result, 0, (int)ringStream.Length);
        CollectionAssert.AreEquivalent(buffer, result);
    }
    
    [Test]
    public void ReadWriteStream()
    {
        var ringStream = new RingStream(10);
        int N = 22;
        var buffer = new byte[N];
        (new Random(3)).NextBytes(buffer);
        var memoryStream = new MemoryStream();
        memoryStream.Write(buffer, 0, buffer.Length);
        memoryStream.Seek(0, SeekOrigin.Begin);
        
        ringStream.Write(memoryStream);
        
        var resultStream = new MemoryStream();

        ringStream.Read(resultStream, (int)ringStream.Length);
        CollectionAssert.AreEquivalent(memoryStream.GetBuffer(), resultStream.GetBuffer());
    }
}