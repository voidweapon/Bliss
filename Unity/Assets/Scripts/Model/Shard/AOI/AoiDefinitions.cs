using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Bliss
{
    /// <summary>
    /// AOI 模块常量。
    /// </summary>
    public static class AoiConstants
    {
        /// <summary>
        /// 网格索引单元边长（世界坐标）。
        /// </summary>
        public const float AOI_GRID_CELL_SIZE = 16f;

        /// <summary>
        /// 查询可见关系时表示“不按 Tag 过滤”。
        /// </summary>
        public const int AOI_TAG_FILTER_ALL = int.MinValue;

        /// <summary>
        /// 新注册 AOI 条目的初始版本号。
        /// </summary>
        public const long AOI_INITIAL_VERSION = 1;
    }

    /// <summary>
    /// AOI 形状类型。
    /// </summary>
    public enum EAoiShapeType : byte
    {
        /// <summary>
        /// 圆形 AOI。
        /// 在实现层按 3D Sphere 语义处理。
        /// </summary>
        Circle = 1,

        /// <summary>
        /// 轴对齐包围盒 AOI。
        /// 在实现层按 3D AABB 语义处理。
        /// </summary>
        Aabb = 2,
    }

    /// <summary>
    /// AOI 3D 网格坐标。
    /// </summary>
    public readonly struct AoiCellKey : IEquatable<AoiCellKey>
    {
        public readonly int CellX;
        public readonly int CellY;
        public readonly int CellZ;

        public AoiCellKey(int cellX, int cellY, int cellZ)
        {
            CellX = cellX;
            CellY = cellY;
            CellZ = cellZ;
        }

        public bool Equals(AoiCellKey other)
        {
            return CellX == other.CellX && CellY == other.CellY && CellZ == other.CellZ;
        }

        public override bool Equals(object obj)
        {
            return obj is AoiCellKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CellX, CellY, CellZ);
        }
    }

    /// <summary>
    /// AOI 注册参数。
    /// </summary>
    public class AoiRegisterInfo
    {
        /// <summary>
        /// 业务实体 Id。
        /// </summary>
        public EntityRef<Entity> EntityRef;

        /// <summary>
        /// 业务 Tag。
        /// </summary>
        public int Tag;

        /// <summary>
        /// AOI 形状。
        /// </summary>
        public EAoiShapeType ShapeType;

        /// <summary>
        /// AOI 中心点（世界坐标）。
        /// </summary>
        public float3 Position;

        /// <summary>
        /// 进入视野半径（ShapeType=Circle 时生效）。
        /// </summary>
        public float EnterRadius;

        /// <summary>
        /// 离开视野半径（ShapeType=Circle 时生效，必须大于等于 EnterRadius）。
        /// </summary>
        public float LeaveRadius;

        /// <summary>
        /// 进入视野 AABB 半尺寸（ShapeType=Aabb 时生效）。
        /// </summary>
        public float3 EnterHalfExtents;

        /// <summary>
        /// 离开视野 AABB 半尺寸（ShapeType=Aabb 时生效，逐轴必须大于等于 EnterHalfExtents）。
        /// </summary>
        public float3 LeaveHalfExtents;

        
        public static AoiRegisterInfo CreateSphere(EntityRef<Entity> entityRef, int tag, float3 position, float enterRadius)
        {
            return new AoiRegisterInfo(entityRef, tag, position, EAoiShapeType.Circle, enterRadius, enterRadius + 1f, float3.zero, float3.zero);
        }
        
        public static AoiRegisterInfo CreateSphere(EntityRef<Entity> entityRef, int tag, float3 position, float enterRadius, float leaveRadius)
        {
            return new AoiRegisterInfo(entityRef, tag, position, EAoiShapeType.Circle, enterRadius, leaveRadius, float3.zero, float3.zero);
        }
        
        public static AoiRegisterInfo CreateAAbb(EntityRef<Entity> entityRef, int tag, float3 position, float3 enterHalfExtents)
        {
            return new AoiRegisterInfo(entityRef, tag, position, EAoiShapeType.Aabb, 0, 0, enterHalfExtents, enterHalfExtents + new float3(1f, 1f, 1f));
        }
        
        public static AoiRegisterInfo CreateAAbb(EntityRef<Entity> entityRef, int tag, float3 position, float3 enterHalfExtents, float3 leaveHalfExtents)
        {
            return new AoiRegisterInfo(entityRef, tag, position, EAoiShapeType.Aabb, 0, 0, enterHalfExtents, leaveHalfExtents);
        }
        
        public AoiRegisterInfo(EntityRef<Entity> entityRef, int tag, float3 position, EAoiShapeType shapeType, float enterRadius, float leaveRadius, float3 enterHalfExtents, float3 leaveHalfExtents)
        {
            this.EntityRef = entityRef;
            this.Tag = tag;
            this.ShapeType = shapeType;
            this.Position = position;
            this.EnterRadius = enterRadius;
            this.LeaveRadius = leaveRadius;
            this.EnterHalfExtents = enterHalfExtents;
            this.LeaveHalfExtents = leaveHalfExtents;
        }
    }

    /// <summary>
    /// AOI 条目运行时数据。
    /// </summary>
    public class AoiEntry
    {
        /// <summary>
        /// AOI 内部句柄，由管理器分配，用于更新和注销。
        /// </summary>
        public long Identifier;

        /// <summary>
        /// 所属实体 Id。
        /// </summary>
        public EntityRef<Entity> EntityRef;

        /// <summary>
        /// 业务 Tag。
        /// </summary>
        public int Tag;

        /// <summary>
        /// AOI 形状。
        /// </summary>
        public EAoiShapeType ShapeType;

        /// <summary>
        /// AOI 中心点（世界坐标）。
        /// </summary>
        public float3 Position;

        /// <summary>
        /// 进入视野半径（ShapeType=Circle 时生效）。
        /// </summary>
        public float EnterRadius;

        /// <summary>
        /// 离开视野半径（ShapeType=Circle 时生效）。
        /// </summary>
        public float LeaveRadius;

        /// <summary>
        /// 进入视野 AABB 半尺寸（ShapeType=Aabb 时生效）。
        /// </summary>
        public float3 EnterHalfExtents;

        /// <summary>
        /// 离开视野 AABB 半尺寸（ShapeType=Aabb 时生效）。
        /// </summary>
        public float3 LeaveHalfExtents;

        /// <summary>
        /// 条目版本号，位置/形状/Tag 变更时递增。
        /// </summary>
        public long Version;

        /// <summary>
        /// 进入视野半径平方缓存，用于快速距离判定。
        /// </summary>
        public float EnterRadiusSquared;

        /// <summary>
        /// 离开视野半径平方缓存，用于维持已建立可见关系。
        /// </summary>
        public float LeaveRadiusSquared;

        /// <summary>
        /// 进入视野 3D 最小包围点。
        /// </summary>
        public float3 EnterMinBounds;

        /// <summary>
        /// 进入视野 3D 最大包围点。
        /// </summary>
        public float3 EnterMaxBounds;

        /// <summary>
        /// 离开视野 3D 最小包围点。
        /// </summary>
        public float3 LeaveMinBounds;

        /// <summary>
        /// 离开视野 3D 最大包围点。
        /// </summary>
        public float3 LeaveMaxBounds;
    }

    /// <summary>
    /// 单个观察者的一次可见关系增量。
    /// </summary>
    public class AoiVisibilityDelta
    {
        /// <summary>
        /// 观察者 AOI 句柄。
        /// </summary>
        public long ObserverIdentifier;

        /// <summary>
        /// 新进入可见集合的目标 AOI 句柄。
        /// </summary>
        public HashSet<long> EnteredTargetIds = new();

        /// <summary>
        /// 离开可见集合的目标 AOI 句柄。
        /// </summary>
        public HashSet<long> LeftTargetIds = new();
    }

    public static class AOIType
    {
        public static readonly int NetworkVisibility = 0;
        public static readonly int Visibility = 1;
        
    }
}
