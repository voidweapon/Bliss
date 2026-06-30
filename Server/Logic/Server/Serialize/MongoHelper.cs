using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Bliss
{
    public static class MongoHelper
    {
        public static byte[] Serialize(object obj)
        {
            TryCallBeforeSerialize(obj);

            return obj.ToBson();
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            return (T)Deserialize(typeof(T), bytes);
        }
        public static object Deserialize(Type type, byte[] bytes)
        {
            object obj = BsonSerializer.Deserialize(bytes, type);
            
            TryCallAfterDeserialize(obj);
            
            return obj;
        }
        
        private static void TryCallBeforeSerialize(object obj)
        {
            if (obj is ISupportSerialize supportSerialize)
            {
                supportSerialize.BeforeSerialize();
            }
        }
        
        private static void TryCallBeforeDeserialize(object obj)
        {
            if (obj is ISupportSerialize supportSerialize)
            {
                supportSerialize.BeginInit();
            }
        }
        
        private static void TryCallAfterDeserialize(object obj)
        {
            if (obj is ISupportSerialize supportSerialize)
            {
                supportSerialize.EndInit();
            }
        }
    }
}