using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.CharacterControllers;
using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    public enum EMovementMode
    {
        None,
        Walking,
        NavWalking,
        Falling,
        Flying,
        Swimming,
        Custom,
        Max,
    }

    public static class CharacterMovementCVars
    {
        public static float NetServerMoveTimestampExpiredWarningThreshold = 1f;
    }
    
    public class CharacterMovementComponent : Entity, IAwake, IDestroy
    {
        public EMovementMode MovementMode;
        public EMovementMode GroundMovementMode; 
        public EMovementMode CustomMovementMode;
        
        
        public int GroundShift;
        public int CustomModeThr;
        public int GroundMask;

        public float3 Gravity = new float3(0, -30f, 0f);
        public float MaxStableMoveSpeed = 10f;

        public float MaxAirMoveSpeed = 10f;
        public float AirAccelerationSpeed = 30f;
        public float AirDrag = 0.1f;

        public uint MaxJumpCount = 2;
        public uint CurrentJumpCount;
        public float JumpUpSpeed = 10f;
        public float JumpScalableForwardSpeed = 0f;
        public bool AllowJumpingWhenSliding;
        public float JumpPostGroundingGraceTime = 0.1f;

        
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;
        public bool IgnoreGravity;
        public bool IgnoreAirDrag;
        public bool IsSmoothRotationChange;
        
        public static readonly float MIN_TICK_TIME = 1e-6f;

        public long LogicTimeStamp;

        public float3 TotalImpose;
        public Dictionary<int, float3> _dicId2ConstantForce = new Dictionary<int, float3>();
        public int _forceIdGenerator;
        
        public HashSet<EntityRef<GameplayCollider>> IgnoredColliders = new HashSet<EntityRef<GameplayCollider>>(EntityRef<GameplayCollider>.Comparer);
        
        public KinematicCharacterComponent KinematicCharacterCmp;
        public CharacterRuntimeData RuntimeDataRef;
        public ICharacterMovementController MovementController;
        public TransformComponent TransformCmp;

        #region Network Movement 
        public bool bReplicateMovement;

        public NetworkPredictionData_Client_CharacterMovement ClientData;
        public NetworkPredictionData_Server_CharacterMovement ServerData;

        public CharacterNetworkMoveDataContainer NetworkMoveDataContainer =  new CharacterNetworkMoveDataContainer();

        public CharacterRepMovement ReplicatedMovement;
        public CharacterNetworkMoveData CurrentNetworkMoveData;
        
        public ENetRole  LocalNetRole;
        public ENetRole  RemoteNetRole;

        public SavedMove_Character CurrentMove;
        public SavedMove_Character OldMove;
        
        public bool IsNetworkSmoothingComplete;
        public ENetworkSmoothingMode  NetworkSmoothingMode;
        public long ServerLastTransformUpdateTimeStamp;
        public long ReplicatedServerLastTransformUpdateTimeStamp;
        public long ServerTimeStampSyncInterval = 0;
        public long ServerTimeStampSyncTimer = 0;
        public bool EnablePredication;
        
        public float MinTimeBetweenTimeStampResets;
        public float LastTimeStampResetServerTime;
        
        
        public float NetworkSimulatedSmoothLocationTime;
        public float NetworkSimulatedSmoothRotationTime;
        /// <summary>
        /// Maximum distance character is allowed to lag behind server location when interpolating between updates.
        /// </summary>
        public float NetworkMaxSmoothUpdateDistance = 2.56f;
        /// <summary>
        /// Maximum distance beyond which character is teleported to the new server location without any smoothing.
        /// </summary>
        public float NetworkNoSmoothUpdateDistance = 3.84f;
        public float ListenServerNetworkSimulatedSmoothLocationTime;
        public float ListenServerNetworkSimulatedSmoothRotationTime;
        #endregion
    }
}