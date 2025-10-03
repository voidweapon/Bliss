using System;
using System.Buffers;

namespace Bliss
{
    public interface ISerializerHelper
    {
        byte[] Serialize(object obj);
        
        void Serialize(object obj, IBufferWriter<byte> writer);
        
        object Deserialize(Type type, byte[] bytes, int offset, int count);
        
        object Deserialize(Type type, ReadOnlySpan<byte> buffer);
        
        object Deserialize(Type type, ReadOnlySequence<byte> buffer);
        
        void Deserialize(Type type, byte[] bytes, int offset, int count, ref object mergeObject);
        
        void Deserialize(Type type, ReadOnlySpan<byte> buffer, ref object mergeObject);
        
        void Deserialize(Type type, ReadOnlySequence<byte> buffer, ref object mergeObject);
    }
}

