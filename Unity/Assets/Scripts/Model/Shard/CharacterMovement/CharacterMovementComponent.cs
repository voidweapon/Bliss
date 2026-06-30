using System.Collections.Generic;
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
        Swimming
    }
    
    public class CharacterMovementComponent : Entity, IAwake, IDestroy
    {
        public EMovementMode MovementMode;

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

        public long LogicTimeStamp;

        public float3 TotalImpose;
        public Dictionary<int, float3> _dicId2ConstantForce = new Dictionary<int, float3>();
        public int _forceIdGenerator;
        
        public HashSet<EntityRef<GameplayCollider>> IgnoredColliders = new HashSet<EntityRef<GameplayCollider>>(EntityRef<GameplayCollider>.Comparer);
        
        public KinematicCharacterComponent KinematicCharacterCmp;
        public CharacterRuntimeData RuntimeDataRef;
        public ICharacterMovementController MovementController;

        #region Network Movement
        public NetworkPredictionData_Client_CharacterMovement ClientData;
        public NetworkPredictionData_Server_CharacterMovement ServerData;
        
        public ENetRole  NetRole;
        public ENetRole  RemoteNetRole;
        
        public bool IsNetworkSmoothingComplete;
        public ENetworkSmoothingMode  NetworkSmoothingMode;
        public long ServerLastTransformUpdateTimeStamp;
        public long ReplicatedServerLastTransformUpdateTimeStamp;
        public long ServerTimeStampSyncInterval = 0;
        public long ServerTimeStampSyncTimer = 0;
        public bool EnablePredication;
        
        public float NetworkMaxSmoothUpdateDistance = 2.56f;
        public float NetworkNoSmoothUpdateDistance = 3.84f;
        #endregion
    }
}