using System;
using System.Buffers;
using MemoryPack;

namespace Bliss
{
    public static class Serializer
    {
        private static readonly MemoryPackSerializerHelperHelper _serializer = new MemoryPackSerializerHelperHelper();
    
        public static byte[] Serialize(object obj)
        {
            TryCallBeforeSerialize(obj);
            
            return _serializer.Serialize(obj);
        }
        
                
        public static void Serialize(object obj, IBufferWriter<byte> writer)
        {
            TryCallBeforeSerialize(obj);

            _serializer.Serialize(obj, writer);
        }

        public static object Deserialize(Type type, byte[] bytes, int offset, int count)
        {
            object obj = _serializer.Deserialize(type, bytes, offset, count);
            
            TryCallAfterSerialize(obj);
            
            return obj;
        }
        
        public static object Deserialize(Type type, ReadOnlySpan<byte> buffer)
        {
            object obj = _serializer.Deserialize(type, buffer);
            
            TryCallAfterSerialize(obj);
            
            return obj;
        }
        
        public static object Deserialize(Type type, ReadOnlySequence<byte> buffer)
        {
            object obj = _serializer.Deserialize(type, buffer);
            
            TryCallAfterSerialize(obj);
            
            return obj;
        }
        
        /// <summary>
        /// 反序列化到<paramref name="mergeObject"/>上，覆盖旧对象内容，需要注意清理旧对象否则会有旧值残留
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="mergeObject"></param>
        /// <returns></returns>
        public static void Deserialize(Type type, byte[] bytes, int offset, int count, ref object mergeObject)
        {
            _serializer.Deserialize(type, bytes,  offset, count, ref mergeObject);
            
            TryCallAfterSerialize(mergeObject);
        }

        /// <summary>
        /// 反序列化到<paramref name="mergeObject"/>上，覆盖旧对象内容，需要注意清理旧对象否则会有旧值残留
        /// </summary>
        /// <param name="type"></param>
        /// <param name="buffer"></param>
        /// <param name="mergeObject"></param>
        /// <returns></returns>
        public static void Deserialize(Type type, ReadOnlySpan<byte> buffer,  ref object mergeObject)
        {
             _serializer.Deserialize(type, buffer,  ref mergeObject);
            
            TryCallAfterSerialize(mergeObject);
        }
        
        /// <summary>
        /// 反序列化到<paramref name="mergeObject"/>上，覆盖旧对象内容，需要注意清理旧对象否则会有旧值残留
        /// </summary>
        /// <param name="type"></param>
        /// <param name="buffer"></param>
        /// <param name="mergeObject"></param>
        /// <returns></returns>
        public static void Deserialize(Type type, ReadOnlySequence<byte> buffer, ref object mergeObject)
        {
           _serializer.Deserialize(type, buffer,  ref mergeObject);
            
            TryCallAfterSerialize(mergeObject);
        }
        
        private static void TryCallBeforeSerialize(object obj)
        {
            if (obj is ISupportSerialize supportSerialize)
            {
                supportSerialize.BeforeSerialize();
            }
        }
        
        private static void TryCallAfterSerialize(object obj)
        {
            if (obj is ISupportSerialize supportSerialize)
            {
                supportSerialize.AfterDeserialize();
            }
        }
        
    }
}

