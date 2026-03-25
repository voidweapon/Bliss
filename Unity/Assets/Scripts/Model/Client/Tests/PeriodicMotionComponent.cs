using Unity.Mathematics;

namespace Bliss
{
    public class PeriodicMotionComponent : Entity, IAwake<PeriodicMotionComponent.InitParam>, IDestroy, IUpdate
    {
        public InitParam InitState;
        
        public ENetRole  NetRole;
        public ENetRole  RemoteNetRole;
        public long StarTime;
        public float3 StartPosition;
        public float3 CurrentVelocity;
        public long OnewayTimer;
        public float OnewayDistance;
        public float DistanceMag;
        
        public long SyncInterval = 100;
        public long LastSyncTime;

        public float Mode2Timer;
        public float EnterMode2Time;
        public bool  InMode2 = false;

        public float3 CurrentAngularVelocity;
        public float RotationTimer;
        public float EnterRotationTimer;
        public bool InStableRotation = false;
        public Unity.Mathematics.Random RotateRandom;
        

        public int TransformSyncTestMode;

        public SavedMove? PendingMove;
        
        public SavedMove NewMove;

        public NetworkPredictionData_Client ClientData;
        public bool IsNetworkSmoothingComplete;
        public ENetworkSmoothingMode  NetworkSmoothingMode;
        public long ServerLastTransformUpdateTimeStamp;
        public long ReplicatedServerLastTransformUpdateTimeStamp;
        public long ServerTimeStampSyncInterval = 0;
        public long ServerTimeStampSyncTimer = 0;
        public float NetworkMaxSmoothUpdateDistance = 2.56f;
        public float NetworkNoSmoothUpdateDistance = 3.84f;
        public bool EnablePredication;
        
        public struct InitParam
        {
            public float Distance;
            public float3 Velocity;
            public float3 AngularVelocity;
            
            public float Mode2Duration;
            public float Mode2Interval;
            public float Mode3IntervalVariance;
            
            public float StableRotationDuration;
            public float ChangeRotationInterval;
            public float ChangeRotationIntervalVariance;
        }
        
        public struct SavedMove
        {
            public bool IsForceNoCombine;
            
            public long TimeStampMS;
            public long DeltaTimeMS;
            
            public float3 StartPosition;
            public quaternion StartRotation;
            public float3 StartVelocity;
            public float3 StartAcceleration;
            public float3 StartAngularVelocity;
            public int StartMoveMode;
            
            public float3 EndPosition;
            public quaternion EndRotation;
            public float3 EndVelocity;
            public float3 EndAcceleration;
            public float3 EndAngularVelocity;
            public int EndMoveMode;
        }

    }
    
    public class NetworkPredictionData_Client
    {
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
        /// Used for remembering how much time has passed between server corrections 
        /// </summary>
        public long LastCorrectionDelta;

        /// <summary>
        /// Used to track time of last correction
        /// </summary>
        public long LastCorrectionTime;

        /// <summary>
        /// Max time delta between server updates over which client smoothing is allowed to interpolate.
        /// Default 0.5f
        /// </summary>
        public long MaxClientSmoothingDeltaTime = 500;

        /// <summary>
        /// Used to track the timestamp of the last server move.
        /// </summary>
        public long SmoothingServerTimeStamp;
        
        /// <summary>
        /// Used to track the client time as we try to match the server.
        /// </summary>
        public long SmoothingClientTimeStamp;

        /// <summary>
        /// Maximum distance character is allowed to lag behind server location when interpolating between updates.
        /// </summary>
        public float MaxSmoothNetUpdateDist;
        
        /// <summary>
        /// Maximum distance beyond which character is teleported to the new server location without any smoothing.
        /// </summary>
        public float NoSmoothNetUpdateDist;

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

        /// <summary>
        ///  Max delta time for a given move, in real seconds
        /// Default 0.125
        /// </summary>
        public long MaxMoveDeltaTime = 125;
    }

    public enum ENetworkSmoothingMode
    {
        Disabled,
        Linear,
        Exponential,
    }
    
    
}