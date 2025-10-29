using System;
using System.Collections.Generic;
using System.Reflection;
using Bliss.Core;
using Bliss.Mathematics;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using Unity.Mathematics;

namespace Bliss.Server
{
    public  static class MongoRegister
    {
        private static void RegisterStruct<T>() where T : struct
        {
            BsonSerializer.RegisterSerializer(typeof (T), new StructBsonSerialize<T>());
        }

        public static void Init()
        {
            // 清理老的数据
            MethodInfo createSerializerRegistry = typeof (BsonSerializer).GetMethod("CreateSerializerRegistry", BindingFlags.Static | BindingFlags.NonPublic);
            createSerializerRegistry.Invoke(null, Array.Empty<object>());
            MethodInfo registerIdGenerators = typeof (BsonSerializer).GetMethod("RegisterIdGenerators", BindingFlags.Static | BindingFlags.NonPublic);
            registerIdGenerators.Invoke(null, Array.Empty<object>());
            
            RegisterStruct<float2>();
            RegisterStruct<float3>();
            RegisterStruct<float4>();
            RegisterStruct<quaternion>();
            RegisterStruct<transform>();
            
            Dictionary<string, Type> types = TypeManager.Instance.GetAllTypes();
            foreach (Type type in types.Values)
            {
                if (!type.IsSubclassOf(typeof (BObject)))
                {
                    continue;
                }

                if (type.IsGenericType || type.IsInterface)
                {
                    continue;
                }

                var classMap = BsonClassMap.LookupClassMap(type);
            }
        }
    }
}

