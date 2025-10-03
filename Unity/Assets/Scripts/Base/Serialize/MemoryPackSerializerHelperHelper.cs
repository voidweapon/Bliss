using System;
using System.Buffers;
using MemoryPack;

namespace Bliss
{
    public class MemoryPackSerializerHelperHelper : ISerializerHelper
    {
        public byte[] Serialize(object obj)
        {
            return MemoryPackSerializer.Serialize(obj.GetType(), obj);
        }

        public void Serialize(object obj, IBufferWriter<byte> writer)
        {
            MemoryPackSerializer.Serialize(obj.GetType(), writer, obj);
        }

        public object Deserialize(Type type, byte[] bytes, int offset, int count)
        {
            return MemoryPackSerializer.Deserialize(type, bytes.AsSpan(offset, count));
        }

        public object Deserialize(Type type, ReadOnlySpan<byte> buffer)
        {
            return MemoryPackSerializer.Deserialize(type, buffer);
        }
        
        public object Deserialize(Type type, ReadOnlySequence<byte> buffer)
        {
            return MemoryPackSerializer.Deserialize(type, buffer);
        }
        
        public void Deserialize(Type type, byte[] bytes, int offset, int count, ref object mergeObject)
        {
            MemoryPackSerializer.Deserialize(type, bytes.AsSpan(offset, count), ref mergeObject);
        }

        public void Deserialize(Type type, ReadOnlySpan<byte> buffer,  ref object mergeObject)
        {
            MemoryPackSerializer.Deserialize(type, buffer, ref mergeObject);
        }
        
        public void Deserialize(Type type, ReadOnlySequence<byte> buffer, ref object mergeObject)
        {
            MemoryPackSerializer.Deserialize(type, buffer, ref mergeObject);
        }
    }
}
