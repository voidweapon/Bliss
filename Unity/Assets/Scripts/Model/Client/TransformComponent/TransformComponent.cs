using System;
using System.Collections.Generic;
using Bliss.Mathematics;
using Unity.Mathematics;

namespace Bliss
{
    public class TransformComponent : Entity
    {
        public transform Transform = new transform(float3.zero);
        
        public Dictionary<object, int> ViewSyncBarrier = new Dictionary<object, int>();

        public transform ViewRelativeTransform = new transform(float3.zero);
    }

    public static class EntityTransformExtension
    {
        public static transform GetTransform(this Entity entity)
        {
            return entity.GetComponent<TransformComponent>()?.Transform ?? new transform(float3.zero);
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