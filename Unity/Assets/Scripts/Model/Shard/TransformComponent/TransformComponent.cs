using System;
using System.Collections.Generic;
using Bliss.Mathematics;
using MemoryPack;
using Unity.Mathematics;

namespace Bliss
{
    using SerializeIgnore = MemoryPackIgnoreAttribute;
    using SerializeInclude = MemoryPackIncludeAttribute;
    
    [MemoryPackable(GenerateType.Object)]
    public partial class TransformComponent : Entity, ISerializableEntity
    {
        [SerializeInclude]
        public transform Transform = new transform(float3.zero);

        [SerializeInclude]
        public transform ViewRelativeTransform = new transform(float3.zero);
        
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
        public Dictionary<object, int> ViewSyncBarrier = new Dictionary<object, int>();
        
    }

    public static class EntityTransformExtension
    {
        public static transform GetTransform(this Entity entity)
        {
            return entity.GetComponent<TransformComponent>()?.Transform ?? new transform(float3.zero);
        }
        
        public static transform GetParentTransform(this Entity self)
        {
            return self.Parent.GetTransform();
        }
    }
        
    
    [Flags]
    public enum ETransformChangeFlag : byte
    {
        None = 0,
        Position = 1 << 0,
        Rotation = 1 << 1,
        Scale = 1 << 2,
    }
    
    public struct Event_TransformChange
    {
        public EntityRef<Entity> Entity;
        public ETransformChangeFlag Flags;
        public float3 Position;
        public quaternion Rotation;
        public float3 Scale;
        public bool IsLocalTransform;
    }
    
    public struct Event_SyncViewTransform
    {
        public EntityRef<Entity> Entity;
        public ETransformChangeFlag Flags;
        public float3 Position;
        public quaternion Rotation;
        public float3 Scale;
        public bool IsLocalTransform;
    }
}