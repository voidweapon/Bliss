using Unity.Mathematics;

namespace Bliss
{
    /// <summary>
    /// 负责将逻辑侧离散的Transform,在视图侧进行平滑
    /// </summary>
    public class SmoothTransformComponent : Entity, IAwake<SmoothTransformComponentInitParam>, IUpdate
    {
        public long ViewTimeStamp;
        
        public float3 CurrentVelocity;
        
        public LogicViewTransformSyncData SyncData;
        
        public bool IsSmoothingComplete;

        public ENetworkSmoothingMode SmoothingMode;
    }

    public class LogicViewTransformSyncData
    {
        /// <summary>
        /// Used to track time of last correction
        /// </summary>
        public long LastCorrectionTime;
        
        /// <summary>
        /// Used for remembering how much time has passed between server corrections 
        /// </summary>
        public long LastCorrectionDelta;
        
        /// <summary>
        /// 
        /// </summary>
        public long LogicTimeStamp;
        
        /// <summary>
        /// 
        /// </summary>
        public long ViewSmoothTimeStamp;
        
        /// <summary>
        /// Used for position smoothing in net games
        /// </summary>
        public float3 OriginalMeshTranslationOffset;
        
        /// <summary>
        /// World space offset of the mesh. Target value is zero offset. Used for position smoothing in net games.
        /// </summary>
        public float3 MeshTranslationOffset;

        /// <summary>
        /// Used for rotation smoothing in net games (only used by linear smoothing).
        /// </summary>
        public quaternion OriginalMeshRotationOffset;
        
        /// <summary>
        ///  Component space offset of the mesh. Used for rotation smoothing in net games.
        /// </summary>
        public quaternion MeshRotationOffset;
        
        /// <summary>
        /// Target for mesh rotation interpolation.
        /// </summary>
        public quaternion MeshRotationTarget;
        
        /// <summary>
        /// Maximum distance character is allowed to lag behind server location when interpolating between updates.
        /// </summary>
        public float MaxSmoothNetUpdateDist;
        
        /// <summary>
        /// Maximum distance beyond which character is teleported to the new server location without any smoothing.
        /// </summary>
        public float NoSmoothNetUpdateDist;
        
        /// <summary>
        /// Max time delta between server updates over which client smoothing is allowed to interpolate.
        /// Default 0.5f
        /// </summary>
        public long MaxClientSmoothingDeltaTime = 500;
        
        /// <summary>
        /// How long to take to smoothly interpolate from the old pawn position on the client to the corrected one sent by the server.  Must be >= 0. Not used for linear smoothing.
        /// Default 0.1S
        /// </summary>
        public long SmoothNetUpdateTime = 100;
        
        /// <summary>
        /// How long to take to smoothly interpolate from the old pawn rotation on the client to the corrected one sent by the server.  Must be >= 0. Not used for linear smoothing.
        /// Default 0.05
        /// </summary>
        public long SmoothNetUpdateRotationTime = 50;
    }

    public struct SmoothTransformComponentInitParam
    {
        public ENetworkSmoothingMode SmoothingMode;
        public float MaxSmoothNetUpdateDist;
        public float NoSmoothNetUpdateDist;

        public SmoothTransformComponentInitParam(ENetworkSmoothingMode smoothingMode)
        {
            SmoothingMode = smoothingMode;
            MaxSmoothNetUpdateDist = 2.56f;
            NoSmoothNetUpdateDist = 3.84f;
        }
        
        public SmoothTransformComponentInitParam(ENetworkSmoothingMode smoothingMode,
             float maxSmoothNetUpdateDist, float noSmoothNetUpdateDist)
        {
            SmoothingMode = smoothingMode;
            MaxSmoothNetUpdateDist = maxSmoothNetUpdateDist;
            NoSmoothNetUpdateDist = noSmoothNetUpdateDist;
        }


    }
}