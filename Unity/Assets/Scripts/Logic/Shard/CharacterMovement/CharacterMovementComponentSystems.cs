using Bliss.CharacterControllers;
using Bliss.Core;
using Bliss.Mathematics;
using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    [EntitySystemOf(typeof(CharacterMovementComponent))]
    public static partial class CharacterMovementComponentSystems
    {
        private static void Awake(this CharacterMovementComponent self)
        {
            self.MovementMode = EMovementMode.None;

            self.LogicTimeStamp = 0;
            self.CurrentJumpCount = 0;
            
            var movementController = new CharacterMovementController();
            movementController.MovementCmp = self;
            self.MovementController = movementController;

            
            self.NetRole = ENetRole.Authority;
            self.RemoteNetRole = ENetRole.Authority;
            var netRoleCmp = self.Parent.GetComponent<NetRoleComponent>();
            if (netRoleCmp != null)
            {
                self.NetRole = netRoleCmp.LocalNetRole;
                self.RemoteNetRole = netRoleCmp.RemoteNetRole;
            }
        }
        private static void Destroy(this CharacterMovementComponent self)
        {
            self.MovementController = null;
        }

        public static void LogicUpdate(this CharacterMovementComponent self, long deltaTimeMS, float deltaTime)
        {
            self.LogicTimeStamp += deltaTimeMS;
            
            if(self.NetRole == ENetRole.SimulatedProxy)
            {
                self.SimulateUpdate();
            }
            else if(self.NetRole >= ENetRole.AutonomousProxy)
            {
                self.AuthorityUpdate();
            }
        }
        
        public static void SetRuntimeData(this CharacterMovementComponent self, CharacterRuntimeData runtimeData)
        {
            self.RuntimeDataRef = runtimeData;
        }

        public static float MaxStableMoveSpeed(this CharacterMovementComponent self)
        {
            return self.MaxStableMoveSpeed;
        }

        #region Force 
        public static  void AddImpose(this CharacterMovementComponent self, float3 impose)
        {
            self.TotalImpose += impose;
        }

        public static  void ClearImpose(this CharacterMovementComponent self)
        {
            self.TotalImpose = float3.zero;
        }

        public static  void SetConstantForce(this CharacterMovementComponent self, ref int forceId, float3 force)
        {
            if (forceId == 0)
            {
                forceId = (++self._forceIdGenerator);
            }

            if (!self._dicId2ConstantForce.TryGetValue(forceId, out var value))
            {
                self._dicId2ConstantForce[forceId] = float3.zero;
            }
            self. _dicId2ConstantForce[forceId] = force;
        }
        
        public static  float3 GetConstantForce(this CharacterMovementComponent self)
        {
            float3 result = float3.zero;
            foreach (var partialForce in self._dicId2ConstantForce)
            {
                result += partialForce.Value;
            }

            return result;
        }
        
        public static  void RemoveConstantForce(this CharacterMovementComponent self, ref int forceId)
        {
            self._dicId2ConstantForce.Remove(forceId);
            forceId = 0;
        }

        public static  void ClearConstantForce(this CharacterMovementComponent self)
        {
            self._dicId2ConstantForce.Clear();
        }

        public static  void ClearForce(this CharacterMovementComponent self)
        {
            self.ClearImpose();
            self.ClearConstantForce();
        }
        #endregion
        
        #region Network Movement

        private static void SimulateUpdate(this CharacterMovementComponent self)
        {
            
        }
        
        private static void AuthorityUpdate(this CharacterMovementComponent self)
        {
            
        }
        #endregion 
    }
    
    public class CharacterMovementController : ICharacterMovementController
    {
        public CharacterMovementComponent MovementCmp;
        public void UpdateRotation(ref quaternion currentRotation, float deltaTime)
        {
            var Motor = MovementCmp.KinematicCharacterCmp;
            var moveInput = MovementCmp.RuntimeDataRef.IntentMovement;
            var OrientationSharpness = MovementCmp.OrientationSharpness;
            float3 forward = Motor.CharacterForward;
            
            if (!moveInput.Equals(float3.zero) && OrientationSharpness > 0f)
            {
                forward = moveInput;
            }

            if (!MovementCmp.IsSmoothRotationChange)
            {
                currentRotation = quaternion.LookRotation(forward, Motor.CharacterUp);
            }
            else
            {
                float3 smoothedLookInputDirection = mathEx.slerp(Motor.CharacterForward, forward, 1f - math.exp(-OrientationSharpness * deltaTime));
                currentRotation = quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }

            if (currentRotation.IsNaN())
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        public void UpdateVelocity(ref float3 currentVelocity, float deltaTime)
        {
            var Motor = MovementCmp.KinematicCharacterCmp;
            var runtimeData = MovementCmp.RuntimeDataRef;
            var moveInput = runtimeData.IntentMovement;
            
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                // Reorient velocity on slope
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * math.length(currentVelocity);

                float3 moveDir = Motor.CharacterForward;
                float moveSpeed = 0f;
                float moveMagnitudeFactor = 1f;
                
                moveDir = math.normalizesafe(moveInput);
                moveSpeed = MovementCmp.MaxStableMoveSpeed();
                moveMagnitudeFactor = math.length(moveInput);
                
                // Calculate target velocity
                float3 inputRight = math.cross(moveDir, Motor.CharacterUp);
                float3 reorientedInput = math.normalizesafe(math.cross(Motor.GroundingStatus.GroundNormal, inputRight)) * moveMagnitudeFactor;
                var targetMovementVelocity = reorientedInput * moveSpeed;
                
                // Smooth movement Velocity
                currentVelocity = math.lerp(currentVelocity, targetMovementVelocity, 1f - math.exp(-MovementCmp.StableMovementSharpness * deltaTime));
            }
            else
            {
                if (math.length(currentVelocity) > 0)
                {
                    float3 addedVelocity = moveInput * MovementCmp.AirAccelerationSpeed * deltaTime;
                    float3  currentVelocityOnInputPlane = mathEx.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                    // Limit air velocity from inputs
                    if (math.length(currentVelocityOnInputPlane) < MovementCmp.MaxAirMoveSpeed)
                    { 
                        // clamp addedVel to make total vel not exceed max vel on inputs plane
                        float3 newTotal = mathEx.ClampMagnitude(currentVelocityOnInputPlane + addedVelocity, MovementCmp.MaxAirMoveSpeed);
                        addedVelocity = newTotal - currentVelocityOnInputPlane;
                    }
                    else
                    {
                        // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                        if (math.dot(currentVelocityOnInputPlane, addedVelocity) > 0)
                        {
                            addedVelocity = mathEx.ProjectOnPlane(addedVelocity, math.normalize(currentVelocityOnInputPlane));
                        }
                    }
                    
                    // Prevent air-climbing sloped walls
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        if (math.dot(currentVelocity + addedVelocity, addedVelocity) > 0)
                        {
                            float3 perpendicularObstructionNormal = math.cross(math.cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp);
                            addedVelocity = mathEx.ProjectOnPlane(addedVelocity, perpendicularObstructionNormal);
                        }
                    }
                    
                    
                    // Apply added velocity
                    currentVelocity += addedVelocity;
                }
                
                if (!MovementCmp.IgnoreGravity)
                {
                    // Gravity
                    currentVelocity += MovementCmp.Gravity * deltaTime;
                }
                
                if (!MovementCmp.IgnoreAirDrag)
                {
                    // Drag
                    currentVelocity *= (1f / (1f + (MovementCmp.AirDrag * deltaTime)));
                }
            }
#if Client
            
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "WantsToJump", runtimeData.WantsToJump, new float4(0, 0, 0, 1), true);
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "JumpConsumed", runtimeData.JumpConsumed, new float4(0, 0, 0, 1), true);
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "JumpedThisFrame", runtimeData.JumpedThisFrame, new float4(0, 0, 0, 1), true);
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "CurrentJumpCount", MovementCmp.CurrentJumpCount, new float4(0, 0, 0, 1), true);
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "TimeSinceLastInJumpableEnvironment", runtimeData.TimeSinceLastInJumpableEnvironment, new float4(0, 0, 0, 1), false);
#endif

            //Jump
            runtimeData.JumpedThisFrame = false;
            if (runtimeData.WantsToJump && !runtimeData.JumpConsumed && MovementCmp.CurrentJumpCount < MovementCmp.MaxJumpCount)
            {
                // See if we actually are allowed to jump
                bool inJumpableEnvironment = MovementCmp.AllowJumpingWhenSliding
                    ? Motor.GroundingStatus.FoundAnyGround
                    : Motor.GroundingStatus.IsStableOnGround;

#if Client
                Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "inJumpableEnvironment", inJumpableEnvironment, new float4(0, 0, 0, 1), true);
#endif
                
                if (inJumpableEnvironment || runtimeData.TimeSinceLastInJumpableEnvironment <= MovementCmp.JumpPostGroundingGraceTime)
                {
                    // Calculate jump direction before ungrounding
                    float3 jumpDirection = Motor.CharacterUp;
                    if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                    {
                        jumpDirection = Motor.GroundingStatus.GroundNormal;
                    }
                    
                    Motor.ForceUnground();
                    
                    currentVelocity += (jumpDirection * MovementCmp.JumpUpSpeed) - math.project(currentVelocity, Motor.CharacterUp);
                    currentVelocity += (math.normalizesafe(moveInput) * MovementCmp.JumpScalableForwardSpeed);
                    
                    runtimeData.JumpedThisFrame = true;
                    runtimeData.JumpConsumed = true;
                    runtimeData.JumpConsumedTimeStamp = TimeInfo.Instance.ClientFrameTime();
                    
                    MovementCmp.CurrentJumpCount++;
                }
            }

            //Force
            float3 totalForce = MovementCmp.GetConstantForce();
            currentVelocity += totalForce * deltaTime;
            if (!MovementCmp.TotalImpose.Equals(float3.zero))
            {
                currentVelocity += MovementCmp.TotalImpose;
                MovementCmp.ClearImpose();
            }

            if (float.IsNaN(currentVelocity.y))
            {
                System.Diagnostics.Debugger.Break();
            }
#if Client
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat("Jump", "currentVelocity", currentVelocity, new float4(0, 0, 0, 1));
#endif
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            var Motor = MovementCmp.KinematicCharacterCmp;
            
            if (Motor.LastGroundingStatus.IsStableOnGround
                && !Motor.GroundingStatus.IsStableOnGround)
            {
                //Leave Ground 
            }
            
            if (!Motor.LastGroundingStatus.IsStableOnGround
                && Motor.GroundingStatus.IsStableOnGround)
            {
                //Land

            }
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            var Motor = MovementCmp.KinematicCharacterCmp;
            var runtimeData = MovementCmp.RuntimeDataRef;
            MovementCmp.RuntimeDataRef.CharacterUp = Motor.CharacterUp;
            
            bool inJumpableEnvironment = MovementCmp.AllowJumpingWhenSliding
                ? Motor.GroundingStatus.FoundAnyGround
                : Motor.GroundingStatus.IsStableOnGround;
            
            if (inJumpableEnvironment)
            {
                // If we're on a ground surface, reset jumping values
                if (!runtimeData.JumpedThisFrame)
                {
                    runtimeData.JumpConsumed = false;
                    runtimeData.JumpConsumedTimeStamp = 0;
                    MovementCmp.CurrentJumpCount = 0;
                }

                runtimeData.TimeSinceLastInJumpableEnvironment = 0f;
            }
            else
            {
                // Keep track of time since we were last able to jump (for grace period)
                runtimeData.TimeSinceLastInJumpableEnvironment += deltaTime;
            }
        }

        public bool IsColliderValidForCollisions(GameplayCollider coll)
        {
            if (MovementCmp.IgnoredColliders.Count == 0)
            {
                return true;
            }
            
            EntityRef<GameplayCollider> collRef = coll;
            return !MovementCmp.IgnoredColliders.Contains(collRef);
        }

        public void OnGroundHit(GameplayCollider hitCollider, float3 hitNormal, float3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void OnMovementHit(GameplayCollider hitCollider, float3 hitNormal, float3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void ProcessHitStabilityReport(GameplayCollider hitCollider, float3 hitNormal, float3 hitPoint,
            float3 atCharacterPosition, quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void OnDiscreteCollisionDetected(GameplayCollider hitCollider)
        {
            
        }

        public long GetLogicTimeStamp()
        {
            return this.MovementCmp.LogicTimeStamp;
        }
    }
    
    
}