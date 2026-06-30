using Bliss.CharacterMovement;
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

        public SavedMove_Character? PendingMove;
        
        public SavedMove_Character NewMove;

        public NetworkPredictionData_Client_CharacterMovement ClientCharacterMovementData;
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
    }
}