using Bliss.Core;

namespace Bliss.Tests;

public class SocketBuffer_Tests
{
    [Test]
    public void WriteSpanTest()
    {
        var skBuf = new SocketBuffer(256);
        byte byteValue = 2;
        bool isTest1 = true;
        bool isTest2 = false;
        short shortValue = 22;
        ushort ushortValue = 100;
        int intValue = -42;
        uint uintValue = 52;
        long longValue = -88;
        ulong ulongValue = 60;

        byte[] except = new byte[256];
        var span = new Span<byte>(except);
        span.WriteIntTo(intValue)
            .WriteBoolTo(isTest1)
            .WriteUIntTo(uintValue)
            .WriteShortTo(shortValue)
            .WriteBoolTo(isTest2)
            .WriteLongTo(longValue)
            .WriteUShortTo(ushortValue)
            .WriteULongTo(ulongValue)
            .WriteByteTo(byteValue);

        int headerLength = sizeof(byte) + sizeof(bool) * 2 + sizeof(short) + sizeof(uint) + sizeof(int) + sizeof(uint) +
                           sizeof(long) + sizeof(ulong);
        skBuf.Reserve(headerLength);
        var testSpan = skBuf.Push(headerLength);
        testSpan.WriteIntTo(intValue)
            .WriteBoolTo(isTest1)
            .WriteUIntTo(uintValue)
            .WriteShortTo(shortValue)
            .WriteBoolTo(isTest2)
            .WriteLongTo(longValue)
            .WriteUShortTo(ushortValue)
            .WriteULongTo(ulongValue)
            .WriteByteTo(byteValue);
        
        CollectionAssert.AreEquivalent(except, skBuf.GetBuffer());
        
    }

    [Test]
    public void WriteReadTest()
    {
        var skBuf = new SocketBuffer(256);
        byte byteValue = 2;
        bool isTest1 = true;
        bool isTest2 = false;
        short shortValue = 22;
        ushort ushortValue = 100;
        int intValue = -42;
        uint uintValue = 52;
        long longValue = -88;
        ulong ulongValue = 60;
        
        int headerLength = sizeof(byte) + sizeof(bool) * 2 + sizeof(short) + sizeof(uint) + sizeof(int) + sizeof(uint) +
                           sizeof(long) + sizeof(ulong);
        skBuf.Reserve(headerLength);
        var pushSpan = skBuf.Push(headerLength);
        pushSpan.WriteIntTo(intValue)
            .WriteBoolTo(isTest1)
            .WriteUIntTo(uintValue)
            .WriteShortTo(shortValue)
            .WriteBoolTo(isTest2)
            .WriteLongTo(longValue)
            .WriteUShortTo(ushortValue)
            .WriteULongTo(ulongValue)
            .WriteByteTo(byteValue);

        var readSpan = skBuf.Pull(headerLength);
        readSpan.ReadInt(out var exceptInt)
            .ReadBool(out var exceptBool1)
            .ReadUInt(out var exceptUInt)
            .ReadShort(out var exceptShort)
            .ReadBool(out var exceptBool2)
            .ReadLong(out var exceptLong)
            .ReadUShort(out var exceptUShort)
            .ReadULong(out var exceptULong)
            .ReadByte(out var exceptByte);

        if (intValue == exceptInt
            && exceptBool1 == isTest1
            && exceptUInt == uintValue
            && exceptShort == shortValue
            && exceptBool2 == isTest2
            && exceptLong == longValue
            && exceptUShort == ushortValue
            && exceptULong == ulongValue
            && exceptByte == byteValue)
        {
            Assert.Pass();
        }
        else
        {
            Assert.Fail();
        }
    }
}