using MemoryPack;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bliss
{
    [MemoryPackUnion(0, typeof(BoxColliderConfig))]
    [MemoryPackUnion(1, typeof(SphereColliderConfig))]
    [MemoryPackUnion(2, typeof(CapsuleColliderConfig))]
    [MemoryPackUnion(3, typeof(MeshColliderConfig))]
    [MemoryPackUnion(4, typeof(HeightFieldConfig))]
    [MemoryPackable(GenerateType.NoGenerate)]
    public abstract partial class PhysicalShapeConfig
    {
        public float3 Location;
        public quaternion Rotation;
        public float3 Scale;
        public bool IsTrigger;
        public int Layer;
        public string Tag;

        public string Name;

        public float ContactOffset;
        
        public int MaterialId;
    }
    
    
    [MemoryPackable(GenerateType.Object)]
    public sealed partial class BoxColliderConfig : PhysicalShapeConfig
    {
        public float3 HalfExtents;
    }
    
    [MemoryPackable(GenerateType.Object)]
    public sealed partial class SphereColliderConfig : PhysicalShapeConfig
    {
        public float Radius;
    }
    
    [MemoryPackable(GenerateType.Object)]
    public sealed partial class CapsuleColliderConfig : PhysicalShapeConfig
    {
        public float Radius;
        public float Height;
        public int Direction; // 0: X, 1: Y, 2: Z
    }
    
    [MemoryPackable(GenerateType.Object)]
    public sealed partial class MeshColliderConfig : PhysicalShapeConfig
    {
        public bool IsConvex;
        public float3[] Vertices;
        public int[] Triangles;
    }
    
        
    [MemoryPackable(GenerateType.Object)]
    public sealed partial class HeightFieldConfig : PhysicalShapeConfig
    {
        public int Width;
        public int Height;
        public short[] Heights;
        public float3 HeightmapScale;
        
    }
    
    [MemoryPackable(GenerateType.Object)]
    public sealed partial class PhysicMaterialConfig
    {
        public int InstanceId;
        public float DynamicFriction;
        public float StaticFriction;
        public float Bounciness;
    }

    [MemoryPackable(GenerateType.Object)]
    public sealed partial class PhysicalRigidActorConfig
    {
        public string Name;
        public float3 Location;
        public quaternion Rotation;
        public bool IsStatic;
        public bool IsKinematic;
        public bool UseGravity;
        public List<PhysicalShapeConfig> Shapes;
    }
    
    [MemoryPackable(GenerateType.Object)]
    public sealed partial class  PhysicalSceneConfig
    {
        public List<PhysicalRigidActorConfig> Actors;
        public List<PhysicMaterialConfig> Materials;
 
    }
}

