using System.Numerics;
using Bliss.Core;
using Bliss.Mathematics;
using Bliss.Physics;
using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    [EntitySystemOf(typeof(KinematicCharacterComponent))]
    public static partial class KinematicCharacterComponentSystems
    {
        private static void Awake(this KinematicCharacterComponent self)
        {
            self.TransformComponent = self.Parent.GetComponent<TransformComponent>();
            self.SmoothTransformComponent = self.Parent.GetComponent<SmoothTransformComponent>();
            
            int unitLayer = self.Parent.GetComponent<GameplayLayer>().Layer;

            self.CollidableLayers = 0;
            for (int i = 0; i < 32; i++)
            {
                if (!GameplayPhysics.GetIgnoreLayerCollision(self.Scene(), unitLayer, i))
                {
                    self.CollidableLayers |= (1 << i);
                }
            }

            self._transientPosition = self.TransformComponent.Transform.Location;
            self.TransientRotation = self.TransformComponent.Transform.Rotation;

            if (self.Capsule != null)
            {
                self.SetCapsuleDimensions(0.5f, 2f, 0f);
            }
            
            self._solveMovementCollisions = true;
            
            self._rigidbodyProjectionHitCount = 0;
            self._overlapsCount = 0;
            self._lastSolvedOverlapNormalDirty = false;
            
            self._mustUnground = false;
            self._mustUngroundTimeCounter = 0f;
            
            self._baseVelocity = float3.zero;
            self._attachedRigidbodyVelocity = float3.zero;
            
            var kinematicMoverManager = self.GetLevelGameplayWorld()?.GetComponent<KinematicMoverManager>();
            kinematicMoverManager?.RegisterCharacterMotor(self);
        }
        private static void Destroy(this KinematicCharacterComponent self)
        {
            self.TransformComponent = null;
            var kinematicMoverManager = self.GetLevelGameplayWorld()?.GetComponent<KinematicMoverManager>();
            kinematicMoverManager?.UnregisterCharacterMotor(self);
        }

        private static bool IsEnable(this KinematicCharacterComponent self)
        {
            return self.Capsule != null;
        }

        
        /// <summary>
        /// Returns all the state information of the motor that is pertinent for simulation
        /// </summary>
        public static KinematicCharacterMotorState GetState(this KinematicCharacterComponent self)
        {
            KinematicCharacterMotorState state = new KinematicCharacterMotorState();

            state.Position = self._transientPosition;
            state.Rotation = self._transientRotation;   

            state.BaseVelocity = self.BaseVelocity;
            state.AttachedRigidbodyVelocity = self._attachedRigidbodyVelocity;

            state.MustUnground = self._mustUnground;
            state.MustUngroundTime = self._mustUngroundTimeCounter;
            state.LastMovementIterationFoundAnyGround = self.LastMovementIterationFoundAnyGround;
            state.GroundingStatus.CopyFrom(self.GroundingStatus);
            state.AttachedRigidbody = self._attachedRigidbody;

            return state;
        }

        /// <summary>
        /// Applies a motor state instantly
        /// </summary>
        public static void ApplyState(this KinematicCharacterComponent self, KinematicCharacterMotorState state, bool bypassInterpolation = true)
        {
            self.SetPositionAndRotation(state.Position, state.Rotation, bypassInterpolation);

            self._baseVelocity = state.BaseVelocity;
            self._attachedRigidbodyVelocity = state.AttachedRigidbodyVelocity;

            self._mustUnground = state.MustUnground;
            self._mustUngroundTimeCounter = state.MustUngroundTime;
            self.LastMovementIterationFoundAnyGround = state.LastMovementIterationFoundAnyGround;
            self.GroundingStatus.CopyFrom(state.GroundingStatus);
            self._attachedRigidbody = state.AttachedRigidbody;
        }
        
        #region Capsule Setting
        
        public static void SetCapsuleDimensions(this KinematicCharacterComponent self,
            float radius, float height, float yOffset)
        {
            self.SetCapsuleDimensionsInner(radius, height, yOffset);

            float realHeight = math.clamp(height, 2 * radius, height);
            float3 center = new float3(0, yOffset, 0);
            self.Capsule.Radius = radius;
            self.Capsule.Height = realHeight;
            self.Capsule.Center = center;
        }

        public static void SetCharacterCapsule(this KinematicCharacterComponent self, GameplayCapsuleCollider gameplayCapsuleCollider)
        {
            self.Capsule = gameplayCapsuleCollider;
            self.SetCapsuleDimensionsInner(gameplayCapsuleCollider.Radius, gameplayCapsuleCollider.Height, gameplayCapsuleCollider.Center.y);
        }
        
        private static void SetCapsuleDimensionsInner(this KinematicCharacterComponent self,
            float radius, float height, float yOffset)
        {
            self.CapsuleRadius = radius;
            self.CapsuleHeight = height;
            self.CapsuleYOffset = yOffset;

            float3 center = new float3(0, yOffset, 0);
            
            self.CharacterTransformToCapsuleCenter = center;
            self.CharacterTransformToCapsuleBottom = center + (-self._cachedWorldUp * (height * 0.5f));
            self.CharacterTransformToCapsuleTop = center + (self._cachedWorldUp * (height * 0.5f));
            self.CharacterTransformToCapsuleBottomHemi = center + (-self._cachedWorldUp * (height * 0.5f)) + (self._cachedWorldUp * radius);
            self.CharacterTransformToCapsuleTopHemi = center + (self._cachedWorldUp * (height * 0.5f)) + (-self._cachedWorldUp * radius);
        }

        #endregion

        #region Transform Utilities
        /// <summary>Sets the character's position directly, including its server physics body.</summary>
        /// <param name="self">The character motor.</param>
        /// <param name="position">The new world position.</param>
        /// <param name="bypassInterpolation">Whether to reset the interpolation starting position.</param>
        public static void SetPosition(this KinematicCharacterComponent self, float3 position, bool bypassInterpolation = true)
        {
            self.TransformComponent.SetPosition(position);
            self._transientPosition = position;
            self.InitialSimulationPosition = position;

#if DOTNET
            GameplayRigidbody body = self.Parent.GetComponent<GameplayRigidbody>();
            if (body != null)
            {
                body.Position = position;
            }
#endif
            if (bypassInterpolation)
            {
                self.InitialTickPosition =  position;
            }
        }
        
        /// <summary>Sets the character's rotation directly, including its server physics body.</summary>
        /// <param name="self">The character motor.</param>
        /// <param name="rotation">The new world rotation.</param>
        /// <param name="bypassInterpolation">Whether to reset the interpolation starting rotation.</param>
        public static void SetRotation(this KinematicCharacterComponent self, quaternion rotation, bool bypassInterpolation = true)
        {
            self.TransformComponent.SetRotation(rotation);
            self.TransientRotation = rotation;
            self.InitialSimulationRotation = rotation;

#if DOTNET
            GameplayRigidbody body = self.Parent.GetComponent<GameplayRigidbody>();
            if (body != null)
            {
                body.Rotation = rotation;
            }
#endif
            if (bypassInterpolation)
            {
                self.InitialTickRotation = rotation;
            }
        }

        /// <summary>Sets the character's world pose directly, including its server physics body.</summary>
        /// <param name="self">The character motor.</param>
        /// <param name="position">The new world position.</param>
        /// <param name="rotation">The new world rotation.</param>
        /// <param name="bypassInterpolation">Whether to reset the interpolation starting pose.</param>
        public static void SetPositionAndRotation(this KinematicCharacterComponent self, float3 position, quaternion rotation, bool bypassInterpolation = true)
        {
            self.TransformComponent.SetPositionAndRotation(position, rotation);
            self._transientPosition = position;
            self.TransientRotation = rotation;
            self.InitialSimulationPosition = position;
            self.InitialSimulationRotation = rotation;

#if DOTNET
            self.syncPhysicsPose(position, rotation);
#endif
            if (bypassInterpolation)
            {
                self.InitialTickPosition = position;
                self.InitialTickRotation = rotation;
            }
        }

#if DOTNET
        internal static void syncPhysicsPose(this KinematicCharacterComponent self, float3 position, quaternion rotation)
        {
            GameplayRigidbody body = self.Parent.GetComponent<GameplayRigidbody>();
            if (body != null)
            {
                body.Rotation = rotation;
                body.Position = position;
            }
        }
#endif

        /// <summary>
        /// Moves the character position, taking all movement collision solving int account. The actual move is done the next time the motor updates are called
        /// </summary>
        /// <param name="self"></param>
        /// <param name="toPosition"></param>
        public static void MoveCharacter(this KinematicCharacterComponent self, float3 toPosition)
        {
            self._movePositionDirty = true;
            self._movePositionTarget = toPosition;
        }
        
        /// <summary>
        /// Moves the character rotation. The actual move is done the next time the motor updates are called
        /// </summary>
        /// <param name="self"></param>
        /// <param name="toRotation"></param>
        public static void RotateCharacter(this KinematicCharacterComponent self, quaternion toRotation)
        {
            self._moveRotationDirty =  true;
            self._moveRotationTarget = toRotation;
        }       
        #endregion
        
        #region Movement Simulate
        /// <summary>
        /// Update phase 1 is meant to be called after physics movers have calculated their velocities, but
        /// before they have simulated their goal positions/rotations. It is responsible for:
        /// - Initializing all values for update
        /// - Handling MovePosition calls
        /// - Solving initial collision overlaps
        /// - Ground probing
        /// - Handle detecting potential interactable rigidbodies
        /// </summary>
        public static void UpdatePhase1(this KinematicCharacterComponent self, float deltaTime)
        {
            // - Initializing all values for update
            
            // NaN propagation safety stop
            if (float.IsNaN(self._baseVelocity.x) || float.IsNaN(self._baseVelocity.y) || float.IsNaN(self._baseVelocity.z))
            {
                self._baseVelocity = float3.zero;
            }
            if (float.IsNaN(self._attachedRigidbodyVelocity.x) || float.IsNaN(self._attachedRigidbodyVelocity.y) || float.IsNaN(self._attachedRigidbodyVelocity.z))
            {
                self._attachedRigidbodyVelocity = float3.zero;
            }
            
            self._rigidbodiesPushedThisMove.Clear();
            
            // Before update Hook
            self.CharacterMovementController.BeforeCharacterUpdate(deltaTime);
            
            var transform = self.TransformComponent.Transform;
            self._transientPosition = transform.Location;
            self.TransientRotation = transform.Rotation;
            self.InitialSimulationPosition = self._transientPosition;
            self.InitialSimulationRotation = self.TransientRotation;
            self._rigidbodyProjectionHitCount = 0;
            self._overlapsCount = 0;
            self._lastSolvedOverlapNormalDirty = false;

            #region Handle Move Position
            if (self._movePositionDirty)
            {
                if (self._solveMovementCollisions)
                {
                    float3 tmpVelocity = self.GetVelocityFromMovement(self._movePositionTarget - self._transientPosition, deltaTime);
                    if (self.InternalCharacterMove(ref tmpVelocity, deltaTime))
                    {
                        if (self.InteractiveRigidbodyHandling)
                        {
                            self.ProcessVelocityForRigidbodyHits(ref tmpVelocity, deltaTime);
                        }
                    }
                }
                else
                {
                    self._transientPosition = self._movePositionTarget;
                }
                self._movePositionDirty = false;
            }
            #endregion
            
            self.LastGroundingStatus.CopyFrom(self.GroundingStatus);
            self.GroundingStatus = new CharacterGroundingReport();
            self.GroundingStatus.GroundNormal = self._characterUp;

            #region Solve Movement Collisionss
            if (self._solveMovementCollisions)
            {
                #region Resolve initial overlaps
                float3 resolutionDirection = self._characterUp;
                float resolutionDistance = 0f;
                int iterationsMade = 0;
                bool overlapSolved = false;
                while (iterationsMade <self. MaxDecollisionIterations && !overlapSolved)
                {
                    int nbOverlaps = self.CharacterCollisionsOverlap(self._transientPosition, self._transientRotation, self._internalProbedColliders);

                    if (nbOverlaps > 0)
                    {
                        // Solve overlaps that aren't against dynamic rigidbodies or physics movers
                        for (int i = 0; i < nbOverlaps; i++)
                        {
                            if (self.GetInteractiveRigidbody(self._internalProbedColliders[i]) == null)
                            {
                                // Process overlap
                                transform overlappedTransform = self._internalProbedColliders[i].GetParentTransform();
                                if (GameplayPhysics.ComputePenetration(self.Scene(),
                                        self.Capsule,
                                        self._transientPosition,
                                        self._transientRotation,
                                        self._internalProbedColliders[i],
                                        overlappedTransform.Location,
                                        overlappedTransform.Rotation,
                                        out resolutionDirection,
                                        out resolutionDistance))
                                {
                                    // Resolve along obstruction direction
                                    HitStabilityReport mockReport = new HitStabilityReport();
                                    mockReport.IsStable = self.IsStableOnNormal(resolutionDirection);
                                    resolutionDirection = self.GetObstructionNormal(resolutionDirection, mockReport.IsStable);

                                    // Solve overlap
                                    float3 resolutionMovement = resolutionDirection * (resolutionDistance + KinematicCharacterComponent.CollisionOffset);
                                    self._transientPosition += resolutionMovement;

                                    // Remember overlaps
                                    if (self._overlapsCount < self._overlaps.Length)
                                    {
                                        self._overlaps[self._overlapsCount] = new OverlapResult(resolutionDirection, self._internalProbedColliders[i]);
                                        self._overlapsCount++;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        overlapSolved = true;
                    }

                    iterationsMade++;
                }
                #endregion
            }
            #endregion

            #region Ground Probing and Snapping
            // Handle ungrounding
            if (self._solveGrounding)
            {
                if (self.MustUnground())
                {
                    self._transientPosition += self._characterUp * (KinematicCharacterComponent.MinimumGroundProbingDistance * 1.5f);
                }
                else
                {
                    // Choose the appropriate ground probing distance
                    float selectedGroundProbingDistance = KinematicCharacterComponent.MinimumGroundProbingDistance;
                    if (!self.LastGroundingStatus.SnappingPrevented && (self.LastGroundingStatus.IsStableOnGround || self.LastMovementIterationFoundAnyGround))
                    {
                        if (self.StepHandling != StepHandlingMethod.None)
                        {
                            selectedGroundProbingDistance = math.max(self.CapsuleRadius, self.MaxStepHeight);;
                        }
                        else
                        {
                            selectedGroundProbingDistance = self.CapsuleRadius;
                        }

                        selectedGroundProbingDistance += self.GroundDetectionExtraDistance;
                    }

                    self.ProbeGround(ref self._transientPosition, self._transientRotation, selectedGroundProbingDistance, ref self.GroundingStatus);

                    if (!self.LastGroundingStatus.IsStableOnGround && self.GroundingStatus.IsStableOnGround)
                    {
                        // Handle stable landing
                        var tempVelocity = mathEx.ProjectOnPlane(self._baseVelocity, self._characterUp);
                        self._baseVelocity = self.GetDirectionTangentToSurface(tempVelocity, self.GroundingStatus.GroundNormal) * math.length(tempVelocity);
                    }
                }
            }

            self.LastMovementIterationFoundAnyGround = false;

            if (self._mustUngroundTimeCounter > 0f)
            {
                self._mustUngroundTimeCounter -= deltaTime;
            }
            self._mustUnground = false;
            #endregion
            
            if (self._solveGrounding)
            {
                self.CharacterMovementController.PostGroundingUpdate(deltaTime);
            }
            
            #region Interactive Rigidbody Handling
            if (self.InteractiveRigidbodyHandling)
            {
                self._lastAttachedRigidbody = self._attachedRigidbody;
                if (self.AttachedRigidbodyOverride != null)
                {
                    self._attachedRigidbody = self.AttachedRigidbodyOverride;
                }
                else
                {
                    // Detect interactive rigidbodies from grounding
                    if (self.GroundingStatus.IsStableOnGround && self.GroundingStatus.GroundCollider.AttachedRigidbody != null)
                    {
                        GameplayRigidbody interactiveRigidbody = self.GetInteractiveRigidbody(self.GroundingStatus.GroundCollider);
                        if (interactiveRigidbody != null)
                        {
                            self._attachedRigidbody = interactiveRigidbody;
                        }
                    }
                    else
                    {
                        self._attachedRigidbody = null;
                    }
                }

                float3 tmpVelocityFromCurrentAttachedRigidbody = float3.zero;
                float3 tmpAngularVelocityFromCurrentAttachedRigidbody = float3.zero;
                if (self._attachedRigidbody != null)
                {
                    self.GetVelocityFromRigidbodyMovement(self._attachedRigidbody, self._transientPosition, deltaTime, out tmpVelocityFromCurrentAttachedRigidbody, out tmpAngularVelocityFromCurrentAttachedRigidbody);
                }

                // Conserve momentum when de-stabilized from an attached rigidbody
                if (self.PreserveAttachedRigidbodyMomentum && self._lastAttachedRigidbody != null && self._attachedRigidbody != self._lastAttachedRigidbody)
                {
                    self._baseVelocity  += self._attachedRigidbodyVelocity;
                    self._baseVelocity  -= tmpVelocityFromCurrentAttachedRigidbody;
                }

                // Process additionnal Velocity from attached rigidbody
                self._attachedRigidbodyVelocity = self._cachedZeroVector;
                if (self._attachedRigidbody != null)
                {
                    self._attachedRigidbodyVelocity = tmpVelocityFromCurrentAttachedRigidbody;

                    // Rotation from attached rigidbody
                    float3 newForward = math.normalizesafe(mathEx.ProjectOnPlane(math.mul(quaternion.Euler(math.degrees(tmpAngularVelocityFromCurrentAttachedRigidbody * deltaTime)), self._characterForward), self._characterUp));
                    self.TransientRotation = quaternion.LookRotation(newForward, self._characterUp);
                }

                // Cancel out horizontal velocity upon landing on an attached rigidbody
                if (self.GroundingStatus.GroundCollider != null &&
                    self.GroundingStatus.GroundCollider.AttachedRigidbody != null &&
                    self.GroundingStatus.GroundCollider.AttachedRigidbody == self._attachedRigidbody &&
                    self._attachedRigidbody != null &&
                    self._lastAttachedRigidbody == null)
                {
                    self._baseVelocity -= mathEx.ProjectOnPlane(self._attachedRigidbodyVelocity, self._characterUp);
                }

                // Movement from Attached Rigidbody
                if (math.lengthsq(self._attachedRigidbodyVelocity) > 0f)
                {
                    self._isMovingFromAttachedRigidbody = true;

                    if (self._solveMovementCollisions)
                    {
                        // Perform the move from rgdbdy velocity
                        self.InternalCharacterMove(ref self._attachedRigidbodyVelocity, deltaTime);
                    }
                    else
                    {
                        self._transientPosition += self._attachedRigidbodyVelocity * deltaTime;
                    }

                    self._isMovingFromAttachedRigidbody = false;
                }
            }
            #endregion
        }

        /// <summary>
        /// Update phase 2 is meant to be called after physics movers have simulated their goal positions/rotations. 
        /// At the end of this, the TransientPosition/Rotation values will be up-to-date with where the motor should be at the end of its move. 
        /// It is responsible for:
        /// - Solving Rotation
        /// - Handle MoveRotation calls
        /// - Solving potential attached rigidbody overlaps
        /// - Solving Velocity
        /// - Applying planar constraint
        /// </summary>
        public static void UpdatePhase2(this KinematicCharacterComponent self, float deltaTime)
        {
            // Handle rotation
            self.CharacterMovementController.UpdateRotation(ref self._transientRotation, deltaTime);
            self.TransientRotation = self._transientRotation;

            // Handle move rotation
            if (self._moveRotationDirty)
            {
                self.TransientRotation = self._moveRotationTarget;
                self._moveRotationDirty = false;
            }

            if (self._solveMovementCollisions && self.InteractiveRigidbodyHandling)
            {
                if (self.InteractiveRigidbodyHandling)
                {
                    #region Solve potential attached rigidbody overlap

                    if (self._attachedRigidbody != null)
                    {
                        float upwardsOffset = self.CapsuleRadius;

                        if (self.CharacterGroundSweep(
                                self._transientPosition + (self._characterUp * upwardsOffset),
                                self._transientRotation,
                                -self._characterUp,
                                upwardsOffset,
                                out var closestHit))
                        {
                            if (((GameplayCollider)closestHit.Collider).AttachedRigidbody == self._attachedRigidbody &&
                                self.IsStableOnNormal(closestHit.normal))
                            {
                                float distanceMovedUp = (upwardsOffset - closestHit.distance);
                                self._transientPosition = self._transientPosition + (self._characterUp * distanceMovedUp) +
                                                          (self._characterUp * KinematicCharacterComponent.CollisionOffset);
                            }
                        }
                    }

                    #endregion
                }

                if (self.InteractiveRigidbodyHandling)
                {
                    #region Resolve overlaps that could've been caused by rotation or physics movers simulation pushing the character
                    float3 resolutionDirection = self._cachedWorldUp;
                    float resolutionDistance = 0f;
                    int iterationsMade = 0;
                    bool overlapSolved = false;
                    while (iterationsMade < self.MaxDecollisionIterations && !overlapSolved)
                    {
                        int nbOverlaps = self.CharacterCollisionsOverlap(self._transientPosition, self._transientRotation, self._internalProbedColliders);
                        if (nbOverlaps > 0)
                        {
                            for (int i = 0; i < nbOverlaps; i++)
                            {
                                // Process overlap
                                var collider =  self._internalProbedColliders[i];
                                var overlappedTransform = collider.GetParentTransform();
                                if (GameplayPhysics.ComputePenetration(self.Scene(),
                                        self.Capsule,
                                        self._transientPosition,
                                        self._transientRotation,
                                        self._internalProbedColliders[i],
                                        overlappedTransform.Location,
                                        overlappedTransform.Rotation,
                                        out resolutionDirection,
                                        out resolutionDistance))
                                {
                                    // Resolve along obstruction direction
                                    HitStabilityReport mockReport = new HitStabilityReport();
                                    mockReport.IsStable = self.IsStableOnNormal(resolutionDirection);
                                    resolutionDirection = self.GetObstructionNormal(resolutionDirection, mockReport.IsStable);
                                    
                                    // Solve overlap
                                    float3 resolutionMovement = resolutionDirection * (resolutionDistance + KinematicCharacterComponent.CollisionOffset);
                                    self._transientPosition += resolutionMovement;
                                    
                                    // If interactiveRigidbody, register as rigidbody hit for velocity
                                    if (self.InteractiveRigidbodyHandling)
                                    {
                                        GameplayRigidbody probedRigidbody = self.GetInteractiveRigidbody(self._internalProbedColliders[i]);
                                        if (probedRigidbody != null)
                                        {
                                            HitStabilityReport tmpReport = new HitStabilityReport();
                                            tmpReport.IsStable = self.IsStableOnNormal(resolutionDirection);
                                            if (tmpReport.IsStable)
                                            {
                                                self.LastMovementIterationFoundAnyGround = true;
                                            }
                                            if (probedRigidbody != self._attachedRigidbody)
                                            {
                                                float3 characterCenter = self._transientPosition + math.mul(self._transientRotation, self.CharacterTransformToCapsuleCenter);
                                                float3 estimatedCollisionPoint =self. _transientPosition;


                                                self.StoreRigidbodyHit(
                                                    probedRigidbody,
                                                    self.Velocity,
                                                    estimatedCollisionPoint,
                                                    resolutionDirection,
                                                    tmpReport);
                                            }
                                        }
                                    }
                                    
                                    // Remember overlaps
                                    if (self._overlapsCount < self._overlaps.Length)
                                    {
                                        self._overlaps[self._overlapsCount] = new OverlapResult(resolutionDirection, self._internalProbedColliders[i]);
                                        self._overlapsCount++;
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            overlapSolved = true;
                        }
                        
                        iterationsMade++;
                    }
                    #endregion
                }
            }
            
            // Handle velocity
            self.CharacterMovementController.UpdateVelocity(ref self._baseVelocity, deltaTime);
            
            if (math.length(self._baseVelocity) < KinematicCharacterComponent.MinVelocityMagnitude)
            {
                self._baseVelocity = float3.zero;
            }
            
            #region Calculate Character movementCharacterGroundSweep from base velocity   
            // Perform the move from base velocity
            if (math.lengthsq(self._baseVelocity) > 0f)
            {
                if (self._solveMovementCollisions)
                {
                    self.InternalCharacterMove(ref self._baseVelocity, deltaTime);
                }
                else
                {
                    self._transientPosition += self._baseVelocity * deltaTime;
                }
            }

            // Process rigidbody hits/overlaps to affect velocity
            if (self.InteractiveRigidbodyHandling)
            {
                self.ProcessVelocityForRigidbodyHits(ref self._baseVelocity, deltaTime);
            }
            #endregion
            
            // Handle planar constraint
            if (self.HasPlanarConstraint)
            {
                self._transientPosition = self.InitialSimulationPosition + mathEx.ProjectOnPlane(self._transientPosition - self.InitialSimulationPosition , math.normalizesafe(self.PlanarConstraintAxis));
            }

            // Discrete collision detection
            if (self.DiscreteCollisionEvents)
            {
                int nbOverlaps = self.CharacterCollisionsOverlap(self._transientPosition, self._transientRotation, self._internalProbedColliders, KinematicCharacterComponent.CollisionOffset * 2f);
                for (int i = 0; i < nbOverlaps; i++)
                {
                    self.CharacterMovementController.OnDiscreteCollisionDetected(self._internalProbedColliders[i]);
                }
            }
            
            self.CharacterMovementController.AfterCharacterUpdate(deltaTime);
        }

        /// <summary>
        /// Moves the character's position by given movement while taking into account all physics simulation, step-handling and 
        /// velocity projection rules that affect the character motor
        /// </summary>
        /// <param name="self"></param>
        /// <param name="transientVelocity"></param>
        /// <param name="deltaTime"></param>
        /// <returns>Returns false if movement could not be solved until the end</returns>
        private static bool InternalCharacterMove(this KinematicCharacterComponent self, ref float3 transientVelocity, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return false;
            }

            // Handle planar constraint
            if (self.HasPlanarConstraint)
            {
                transientVelocity = math.project(transientVelocity, self.PlanarConstraintAxis);
            }
            
            
            bool wasCompleted = true;
            float3 remainingMovementDirection = math.normalizesafe(transientVelocity);
            float remainingMovementMagnitude = math.length(transientVelocity) * deltaTime;
            float3 originalVelocityDirection = remainingMovementDirection;
            int sweepsMade = 0;
            bool hitSomethingThisSweepIteration = true;
            float3 tmpMovedPosition = self._transientPosition;
            bool previousHitIsStable = false;
            float3 previousVelocity = self._cachedZeroVector;
            float3 previousObstructionNormal =  self._cachedZeroVector;
            MovementSweepState sweepState = MovementSweepState.Initial;
            
            
            // Solving initial collision overlaps
            // Project movement against current overlaps before doing the sweeps
            for (int i = 0; i < self.OverlapsCount; i++)
            {
                float3 overlapNormal = self._overlaps[i].Normal;
                if (math.dot(remainingMovementDirection, overlapNormal) < 0f)
                {
                    bool stableOnHit = self.IsStableOnNormal(overlapNormal) && !self.MustUnground();
                    float3 velocityBeforeProjection = transientVelocity;
                    float3 obstructionNormal = self.GetObstructionNormal(overlapNormal, stableOnHit);

                    self.InternalHandleVelocityProjection(
                        stableOnHit,
                        overlapNormal,
                        obstructionNormal,
                        originalVelocityDirection,
                        ref sweepState,
                        previousHitIsStable,
                        previousVelocity,
                        previousObstructionNormal,
                        ref transientVelocity,
                        ref remainingMovementMagnitude,
                        ref remainingMovementDirection);
                    
                    previousHitIsStable = stableOnHit;
                    previousVelocity = velocityBeforeProjection;
                    previousObstructionNormal = obstructionNormal;
                }
            }
            
            // Sweep the desired movement to detect collisions
            while (remainingMovementMagnitude > 0f &&
                   (sweepsMade <= self.MaxMovementIterations) &&
                   hitSomethingThisSweepIteration)
            {
                bool foundClosestHit = false;
                float3 closestSweepHitPoint = default;
                float3 closestSweepHitNormal = default;
                float closestSweepHitDistance = 0f;
                GameplayCollider closestSweepHitCollider = null;

                if (self.CheckMovementInitialOverlaps)
                {
                    int numOverlaps = self.CharacterCollisionsOverlap(
                        tmpMovedPosition,
                        self._transientRotation,
                        self._internalProbedColliders,
                        0f,
                        false);

                    if (numOverlaps > 0)
                    {
                        closestSweepHitDistance = 0f;

                        float mostObstructingOverlapNormalDotProduct = 2f;

                        for (int i = 0; i < numOverlaps; i++)
                        {
                            var tmpCollider = self._internalProbedColliders[i];
                            var tmpColliderTm = tmpCollider.Parent.GetComponent<TransformComponent>().Transform;

                            if (GameplayPhysics.ComputePenetration(self.Scene(),
                                    self.Capsule,
                                    tmpMovedPosition,
                                    self._transientRotation,
                                    tmpCollider,
                                    tmpColliderTm.Location,
                                    tmpColliderTm.Rotation,
                                    out float3 resolutionDirection,
                                    out float resolutionDistance))
                            {
                                float dotProduct = math.dot(remainingMovementDirection, resolutionDirection);
                                if (dotProduct < 0f && dotProduct < mostObstructingOverlapNormalDotProduct)
                                {
                                    mostObstructingOverlapNormalDotProduct = dotProduct;

                                    closestSweepHitNormal = resolutionDirection;
                                    closestSweepHitCollider = tmpCollider;
                                    closestSweepHitPoint = tmpMovedPosition + math.mul(self._transientRotation, self.CharacterTransformToCapsuleCenter) + (resolutionDirection * resolutionDistance);

                                    foundClosestHit = true;
                                }

                            }
                        }
                    }
                }

                if (!foundClosestHit && self.CharacterCollisionsSweep(
                        tmpMovedPosition,
                        self._transientRotation,
                        remainingMovementDirection,
                        remainingMovementMagnitude + KinematicCharacterComponent.CollisionOffset,
                        out GameplayRaycastHit closestSweepHit,
                        self._internalCharacterHits) 
                    > 0)
                {
                    closestSweepHitNormal = closestSweepHit.normal;
                    closestSweepHitDistance = closestSweepHit.distance;
                    closestSweepHitCollider = (GameplayCollider)closestSweepHit.Collider;
                    closestSweepHitPoint = closestSweepHit.point;
                    
                    foundClosestHit = true;
                }

                if (foundClosestHit)
                {
                    // Calculate movement from this iteration
                    float3 sweepMovement = (remainingMovementDirection * (math.max(0f, closestSweepHitDistance - KinematicCharacterComponent.CollisionOffset)));
                    tmpMovedPosition += sweepMovement;
                    remainingMovementMagnitude -= math.length(sweepMovement);
                        
                    // Evaluate if hit is stable
                    HitStabilityReport moveHitStabilityReport = new HitStabilityReport();
                    self.EvaluateHitStability(closestSweepHitCollider, closestSweepHitNormal, closestSweepHitPoint, tmpMovedPosition, self._transientRotation, transientVelocity, ref moveHitStabilityReport);
                        
                    // Handle stepping up steps points higher than bottom capsule radius
                    bool foundValidStepHit = false;
                    if (self._solveGrounding && self.StepHandling != StepHandlingMethod.None && moveHitStabilityReport.ValidStepDetected)
                    {
                        float obstructionCorrelation = math.abs(math.dot(closestSweepHitNormal, self._characterUp));
                        if (obstructionCorrelation <= KinematicCharacterComponent.CorrelationForVerticalObstruction)
                        {
                            float3 stepForwardDirection = math.normalizesafe(mathEx.ProjectOnPlane(-closestSweepHitNormal, self._characterUp));
                            float3 stepCastStartPoint = (tmpMovedPosition + (stepForwardDirection * KinematicCharacterComponent.SteppingForwardDistance)) +
                                                        (self._characterUp * self.MaxStepHeight);

                            // Cast downward from the top of the stepping height
                            int nbStepHits = self.CharacterCollisionsSweep(
                                stepCastStartPoint, // position
                                self._transientRotation, // rotation
                                -self._characterUp, // direction
                                self.MaxStepHeight, // distance
                                out GameplayRaycastHit closestStepHit, // closest hit
                                self._internalCharacterHits,
                                0f,
                                true); // all hits 

                            // Check for hit corresponding to stepped collider
                            for (int i = 0; i < nbStepHits; i++)
                            {
                                if ((GameplayCollider)self._internalCharacterHits[i].Collider == moveHitStabilityReport.SteppedCollider)
                                {
                                    float3 endStepPosition = stepCastStartPoint + (-self._characterUp * (self._internalCharacterHits[i].distance - KinematicCharacterComponent.CollisionOffset));
                                    tmpMovedPosition = endStepPosition;
                                    foundValidStepHit = true;

                                    // Project velocity on ground normal at step
                                    transientVelocity = mathEx.ProjectOnPlane(transientVelocity, self._characterUp);
                                    remainingMovementDirection = math.normalizesafe(transientVelocity);

                                    break;
                                }
                            }
                        }
                    }
                        
                    // Handle movement solving
                    if (!foundValidStepHit)
                    {
                        float3 obstructionNormal = self.GetObstructionNormal(closestSweepHitNormal, moveHitStabilityReport.IsStable);

                        // Movement hit callback
                        self.CharacterMovementController.OnMovementHit(closestSweepHitCollider, closestSweepHitNormal, closestSweepHitPoint, ref moveHitStabilityReport);

                        // Handle remembering rigidbody hits
                        if (self.InteractiveRigidbodyHandling && closestSweepHitCollider.AttachedRigidbody != null)
                        {
                            self.StoreRigidbodyHit(
                                closestSweepHitCollider.AttachedRigidbody,
                                transientVelocity,
                                closestSweepHitPoint,
                                obstructionNormal,
                                moveHitStabilityReport);
                        }

                        bool stableOnHit = moveHitStabilityReport.IsStable && ! self.MustUnground();
                        float3 velocityBeforeProj = transientVelocity;

                        // Project velocity for next iteration
                        self.InternalHandleVelocityProjection(
                            stableOnHit,
                            closestSweepHitNormal,
                            obstructionNormal,
                            originalVelocityDirection,
                            ref sweepState,
                            previousHitIsStable,
                            previousVelocity,
                            previousObstructionNormal,
                            ref transientVelocity,
                            ref remainingMovementMagnitude,
                            ref remainingMovementDirection);

                        previousHitIsStable = stableOnHit;
                        previousVelocity = velocityBeforeProj;
                        previousObstructionNormal = obstructionNormal;
                    }
                }
                else
                {
                    // If we hit nothing...
                    hitSomethingThisSweepIteration = false;
                }
                    
                // Safety for exceeding max sweeps allowed
                sweepsMade++;
                if (sweepsMade > self.MaxMovementIterations)
                {
                    if (self.KillRemainingMovementWhenExceedMaxMovementIterations)
                    {
                        remainingMovementMagnitude = 0f;
                    }

                    if (self.KillVelocityWhenExceedMaxMovementIterations)
                    {
                        transientVelocity = float3.zero;;
                    }
                    wasCompleted = false;
                }
            }
            
            // Move position for the remainder of the movement
            tmpMovedPosition += (remainingMovementDirection * remainingMovementMagnitude);
            self._transientPosition = tmpMovedPosition;
            
            return wasCompleted;
        }

        private static void InternalHandleVelocityProjection(this KinematicCharacterComponent self, 
            bool stableOnHit, float3 hitNormal,
            float3 obstructionNormal, float3 originalDirection,
            ref MovementSweepState sweepState, 
            bool previousHitIsStable, float3 previousVelocity, float3 previousObstructionNormal,
            ref float3 transientVelocity, ref float remainingMovementMagnitude, ref float3 remainingMovementDirection)
        {
            if (math.lengthsq(transientVelocity) <= 0f)
            {
                return;
            }
            
            float3 velocityBeforeProjection = transientVelocity;

            if (stableOnHit)
            {
                self.LastMovementIterationFoundAnyGround = true;
                self.HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
            }
            else
            {
                if (sweepState == MovementSweepState.Initial)
                {
                    // Handle projection
                    self.HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
                    sweepState = MovementSweepState.AfterFirstHit;
                }
                else if (sweepState == MovementSweepState.AfterFirstHit)
                {
                    // Blocking crease handling
                    self.EvaluateCrease(
                        transientVelocity,
                        previousVelocity,
                        obstructionNormal,
                        previousObstructionNormal,
                        stableOnHit,
                        previousHitIsStable,
                        self.GroundingStatus.IsStableOnGround && !self.MustUnground(),
                        out bool foundCrease,
                        out float3 creaseDirection);

                    if (foundCrease)
                    {
                        if (self.GroundingStatus.IsStableOnGround && !self.MustUnground())
                        {
                            transientVelocity = float3.zero;
                            sweepState = MovementSweepState.FoundBlockingCorner;
                        }
                        else
                        {
                            transientVelocity = math.project(transientVelocity, creaseDirection);
                            sweepState = MovementSweepState.FoundBlockingCrease;
                        }
                    }
                    else
                    {
                        // Handle projection
                        self.HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
                    }
                }
                else if (sweepState == MovementSweepState.FoundBlockingCrease)
                {
                    // Blocking corner handling
                    transientVelocity = float3.zero;
                    sweepState = MovementSweepState.FoundBlockingCorner;
                }
            }

            if (self.HasPlanarConstraint)
            {
                transientVelocity = mathEx.ProjectOnPlane(transientVelocity, math.normalizesafe(self.PlanarConstraintAxis));
            }
            
            float newVelocityFactor = math.length(transientVelocity) / math.length(velocityBeforeProjection);
            remainingMovementMagnitude *= newVelocityFactor;
            remainingMovementDirection = math.normalizesafe(transientVelocity);
        }

        private static void EvaluateCrease(this KinematicCharacterComponent self,
            float3 currentCharacterVelocity,
            float3 previousCharacterVelocity,
            float3 currentHitNormal,
            float3 previousHitNormal,
            bool currentHitIsStable,
            bool previousHitIsStable,
            bool characterIsStable,
            out bool isValidCrease,
            out float3 creaseDirection)
        {
            isValidCrease = false;
            creaseDirection = default;
            
            if (!characterIsStable || !currentHitIsStable || !previousHitIsStable)
            {
                float3 tmpBlockingCreaseDirection = math.normalizesafe(math.cross(currentHitNormal, previousHitNormal));
                float dotPlanes = math.dot(currentHitNormal, previousHitNormal);
                bool isVelocityConstrainedByCrease = false;

                // Avoid calculations if the two planes are the same
                if (dotPlanes < 0.999f)
                {
                    // TODO: can this whole part be made simpler? (with 2d projections, etc)
                    float3 normalAOnCreasePlane = math.normalizesafe(mathEx.ProjectOnPlane(currentHitNormal, tmpBlockingCreaseDirection));
                    float3 normalBOnCreasePlane = math.normalizesafe(mathEx.ProjectOnPlane(previousHitNormal, tmpBlockingCreaseDirection));
                    float dotPlanesOnCreasePlane = math.dot(normalAOnCreasePlane, normalBOnCreasePlane);

                    float3 enteringVelocityDirectionOnCreasePlane = math.normalizesafe(mathEx.ProjectOnPlane(previousCharacterVelocity, tmpBlockingCreaseDirection));

                    if (dotPlanesOnCreasePlane <= (math.dot(-enteringVelocityDirectionOnCreasePlane, normalAOnCreasePlane) + 0.001f) &&
                        dotPlanesOnCreasePlane <= (math.dot(-enteringVelocityDirectionOnCreasePlane, normalBOnCreasePlane) + 0.001f))
                    {
                        isVelocityConstrainedByCrease = true;
                    }
                }

                if (isVelocityConstrainedByCrease)
                {
                    // Flip crease direction to make it representative of the real direction our velocity would be projected to
                    if (math.dot(tmpBlockingCreaseDirection, currentCharacterVelocity) < 0f)
                    {
                        tmpBlockingCreaseDirection = -tmpBlockingCreaseDirection;
                    }

                    isValidCrease = true;
                    creaseDirection = tmpBlockingCreaseDirection;
                }
            }
        }

        public static void HandleVelocityProjection(this KinematicCharacterComponent self, ref float3 velocity, float3 obstructionNormal, bool stableOnHit)
        {
            if (self.GroundingStatus.IsStableOnGround && !self.MustUnground())
            {
                if (stableOnHit)
                {
                    // On stable slopes, simply reorient the movement without any loss
                    velocity = self.GetDirectionTangentToSurface(velocity, obstructionNormal) * math.length(velocity);
                }
                else
                {
                    // On blocking hits, project the movement on the obstruction while following the grounding plane
                    float3 obstructionRightAlongGround = math.normalizesafe(math.cross(obstructionNormal, self.GroundingStatus.GroundNormal));
                    float3 obstructionUpAlongGround = math.normalizesafe(math.cross(obstructionRightAlongGround, obstructionNormal));
                    velocity = self.GetDirectionTangentToSurface(velocity, obstructionUpAlongGround) * math.length(velocity);
                    velocity = mathEx.ProjectOnPlane(velocity, obstructionNormal);
                }
            }
            else
            {
                if (stableOnHit)
                {
                    // Handle stable landing
                    velocity = mathEx.ProjectOnPlane(velocity, self._characterUp);
                    velocity = self.GetDirectionTangentToSurface(velocity, obstructionNormal) * math.length(velocity);

                }
                else
                {
                    // Handle generic obstruction
                    velocity = mathEx.ProjectOnPlane(velocity, obstructionNormal);
                }
            }
        }

        /// <summary>
        /// Takes into account rigidbody hits for adding to the velocity
        /// </summary>
        private static void ProcessVelocityForRigidbodyHits(this KinematicCharacterComponent self, ref float3 processedVelocity, float deltaTime)
        {
            for (int i = 0; i < self._rigidbodyProjectionHitCount; i++)
            {
                RigidbodyProjectionHit bodyHit = self._internalRigidbodyProjectionHits[i];

                if (bodyHit.Rigidbody != null && !self._rigidbodiesPushedThisMove.Contains(bodyHit.Rigidbody))
                {
                    if (self._internalRigidbodyProjectionHits[i].Rigidbody != self._attachedRigidbody)
                    {
                        // Remember we hit this rigidbody
                        self._rigidbodiesPushedThisMove.Add(bodyHit.Rigidbody);

                        float characterMass = self.SimulatedCharacterMass;
                        float3 characterVelocity = bodyHit.HitVelocity;

                        var hitCharacterMotor = bodyHit.Rigidbody.Parent.GetComponent<KinematicCharacterComponent>();
                        bool hitBodyIsCharacter = hitCharacterMotor != null;
                        bool hitBodyIsDynamic = !bodyHit.Rigidbody.isKinematic;
                        float hitBodyMass = bodyHit.Rigidbody.Mass;
                        float hitBodyMassAtPoint = bodyHit.Rigidbody.Mass; // todo
                        float3 hitBodyVelocity = bodyHit.Rigidbody.Velocity;
                        if (hitBodyIsCharacter)
                        {
                            hitBodyMass = hitCharacterMotor.SimulatedCharacterMass;
                            hitBodyMassAtPoint = hitCharacterMotor.SimulatedCharacterMass; // todo
                            hitBodyVelocity = hitCharacterMotor.BaseVelocity;
                        }
                        else if (!hitBodyIsDynamic)
                        {
                            KinematicRigidbodyMover physicsMover = bodyHit.Rigidbody.Parent.GetComponent<KinematicRigidbodyMover>();
                            if(physicsMover != null)
                            {
                                hitBodyVelocity = physicsMover.Velocity;
                            }
                        }

                        // Calculate the ratio of the total mass that the character mass represents
                        float characterToBodyMassRatio = 1f;
                        {
                            if (characterMass + hitBodyMassAtPoint > 0f)
                            {
                                characterToBodyMassRatio = characterMass / (characterMass + hitBodyMassAtPoint);
                            }
                            else
                            {
                                characterToBodyMassRatio = 0.5f;
                            }

                            // Hitting a non-dynamic body
                            if (!hitBodyIsDynamic)
                            {
                                characterToBodyMassRatio = 0f;
                            }
                            // Emulate kinematic body interaction
                            else if (self.RigidbodyInteractionType == RigidbodyInteractionType.Kinematic && !hitBodyIsCharacter)
                            {
                                characterToBodyMassRatio = 1f;
                            }
                        }

                        self.ComputeCollisionResolutionForHitBody(
                            bodyHit.EffectiveHitNormal,
                            characterVelocity,
                            hitBodyVelocity,
                            characterToBodyMassRatio,
                            out float3 velocityChangeOnCharacter,
                            out float3 velocityChangeOnBody);

                        processedVelocity += velocityChangeOnCharacter;

                        if (hitBodyIsCharacter)
                        {
                            hitCharacterMotor._baseVelocity += velocityChangeOnCharacter;
                        }
                        else if (hitBodyIsDynamic)
                        {
                           bodyHit.Rigidbody.AddForceAtPosition(velocityChangeOnBody, bodyHit.HitPoint, ForceMode.VelocityChange);
                        }

                        if (self.RigidbodyInteractionType == RigidbodyInteractionType.SimulatedDynamic)
                        {
                            self.HandleSimulatedRigidbodyInteraction(ref processedVelocity, bodyHit, deltaTime);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Allows you to override the way hit rigidbodies are pushed / interacted with. 
        /// ProcessedVelocity is what must be modified if this interaction affects the character's velocity.
        /// </summary>
        public static void HandleSimulatedRigidbodyInteraction(this KinematicCharacterComponent self, ref float3 processedVelocity, RigidbodyProjectionHit hit, float deltaTime)
        {
            
        }

        public static void ComputeCollisionResolutionForHitBody(this KinematicCharacterComponent self,
            float3 hitNormal,
            float3 characterVelocity,
            float3 bodyVelocity,
            float characterToBodyMassRatio,
            out float3 velocityChangeOnCharacter,
            out float3 velocityChangeOnBody)
        {
            velocityChangeOnCharacter = default;
            velocityChangeOnBody = default;

            float bodyToCharacterMassRatio = 1f - characterToBodyMassRatio;
            float characterVelocityMagnitudeOnHitNormal = math.dot(characterVelocity, hitNormal);
            float bodyVelocityMagnitudeOnHitNormal = math.dot(bodyVelocity, hitNormal);

            // if character velocity was going against the obstruction, restore the portion of the velocity that got projected during the movement phase
            if (characterVelocityMagnitudeOnHitNormal < 0f)
            {
                float3 restoredCharacterVelocity = hitNormal * characterVelocityMagnitudeOnHitNormal;
                velocityChangeOnCharacter += restoredCharacterVelocity;
            }

            // solve impulse velocities on both bodies, but only if the body velocity would be giving resistance to the character in any way
            if (bodyVelocityMagnitudeOnHitNormal > characterVelocityMagnitudeOnHitNormal)
            {
                float3 relativeImpactVelocity = hitNormal * (bodyVelocityMagnitudeOnHitNormal - characterVelocityMagnitudeOnHitNormal);
                velocityChangeOnCharacter += relativeImpactVelocity * bodyToCharacterMassRatio;
                velocityChangeOnBody += -relativeImpactVelocity * characterToBodyMassRatio;
            }
        }
        
        /// <summary>
        /// Gets the effective normal for movement obstruction depending on current grounding status
        /// </summary>
        private static float3 GetObstructionNormal(this KinematicCharacterComponent self, float3 hitNormal, bool stableOnHit)
        {
            // Find hit/obstruction/offset normal
            float3 obstructionNormal = hitNormal;
            if (self.GroundingStatus.IsStableOnGround && !self.MustUnground() && !stableOnHit)
            {
                float3 obstructionLeftAlongGround = math.normalizesafe(math.cross(self.GroundingStatus.GroundNormal, obstructionNormal));
                obstructionNormal = math.normalizesafe(math.cross(obstructionLeftAlongGround, self._characterUp));
            }

            // Catch cases where cross product between parallel normals returned 0
            if (math.lengthsq(obstructionNormal) == 0f)
            {
                obstructionNormal = hitNormal;
            }

            return obstructionNormal;
        }
        
        /// <summary>
        /// Remembers a rigidbody hit for processing later
        /// </summary>
        private static void StoreRigidbodyHit(this KinematicCharacterComponent self, GameplayRigidbody hitRigidbody, float3 hitVelocity, float3 hitPoint, float3 obstructionNormal, HitStabilityReport hitStabilityReport)
        {
            if (self._rigidbodyProjectionHitCount < self._internalRigidbodyProjectionHits.Length)
            {
                if (hitRigidbody.Parent.GetComponent<KinematicCharacterComponent>() == null)
                {
                    RigidbodyProjectionHit rph = new RigidbodyProjectionHit();
                    rph.Rigidbody = hitRigidbody;
                    rph.HitPoint = hitPoint;
                    rph.EffectiveHitNormal = obstructionNormal;
                    rph.HitVelocity = hitVelocity;
                    rph.StableOnHit = hitStabilityReport.IsStable;

                    self._internalRigidbodyProjectionHits[self._rigidbodyProjectionHitCount] = rph;
                    self._rigidbodyProjectionHitCount++;
                }
            }
        }

        /// <summary>
        /// Returns the direction adjusted to be tangent to a specified surface normal relatively to the character's up direction.
        /// Useful for reorienting a direction on a slope without any lateral deviation in trajectory
        /// </summary>
        public static float3 GetDirectionTangentToSurface(this KinematicCharacterComponent self, float3 velocity, float3 surfaceNormal)
        {
            float3 directionRight = math.cross(velocity, self._characterUp);
            return math.normalizesafe(math.cross(surfaceNormal, directionRight));
        }
        #endregion
        
        /// <summary>
        /// Calculates the velocity required to move the character to the target position over a specific deltaTime.
        /// Useful for when you wish to work with positions rather than velocities in the UpdateVelocity callback 
        /// </summary>
        public static float3 GetVelocityForMovePosition(this KinematicCharacterComponent self, float3 fromPosition, float3 toPosition, float deltaTime)
        {
            return self.GetVelocityFromMovement(toPosition - fromPosition, deltaTime);
        }

        public static float3 GetVelocityFromMovement(this KinematicCharacterComponent self, float3 movement, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return float3.zero;
            }
            return movement / deltaTime;
        }
        
        /// <summary>
        /// Determines if a collider has an attached interactive rigidbody
        /// </summary>
        private static GameplayRigidbody GetInteractiveRigidbody(this KinematicCharacterComponent self, GameplayCollider onCollider)
        {
            GameplayRigidbody colliderAttachedRigidbody = onCollider.AttachedRigidbody;
            if (colliderAttachedRigidbody != null)
            {
                if (colliderAttachedRigidbody.Parent.GetComponent<KinematicRigidbodyMover>() != null)
                {
                    return colliderAttachedRigidbody;
                }

                if (!colliderAttachedRigidbody.isKinematic)
                {
                    return colliderAttachedRigidbody;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Determines if motor can be considered stable on given slope normal
        /// </summary>
        private static bool IsStableOnNormal(this KinematicCharacterComponent self, float3 normal)
        {
            return mathEx.angle(self._characterUp, normal) <= self.MaxStableSlopeAngle;
        }

        /// <summary>
        /// Determines if motor can be considered stable on given slope normal
        /// </summary>
        private static bool IsStableWithSpecialCases(this KinematicCharacterComponent self, ref HitStabilityReport stabilityReport, float3 velocity)
        {
            if (self.LedgeAndDenivelationHandling)
            {
                if (stabilityReport.LedgeDetected)
                {
                    if (stabilityReport.IsMovingTowardsEmptySideOfLedge)
                    {
                        // Max snap vel
                        float3 velocityOnLedgeNormal = math.project(velocity, stabilityReport.LedgeFacingDirection);
                        if (math.length(velocityOnLedgeNormal) > self.MaxVelocityForLedgeSnap)
                        {
                            return false;
                        }
                    }
                    
                    // Distance from ledge
                    if (stabilityReport.IsOnEmptySideOfLedge && stabilityReport.DistanceFromLedge > self.MaxStableDistanceFromLedge)
                    {
                        return false;
                    }
                }
                
                // "Launching" off of slopes of a certain denivelation angle
                if (self.LastGroundingStatus.FoundAnyGround && math.lengthsq(stabilityReport.InnerNormal) != 0f && math.lengthsq(stabilityReport.OuterNormal) != 0f)
                {
                    float denivelationAngle = mathEx.angle(stabilityReport.InnerNormal, stabilityReport.OuterNormal);
                    if (denivelationAngle > self.MaxStableDenivelationAngle)
                    {
                        return false;
                    }
                    else
                    {
                        denivelationAngle = mathEx.angle(self.LastGroundingStatus.InnerGroundNormal, stabilityReport.OuterNormal);
                        if (denivelationAngle > self.MaxStableDenivelationAngle)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Probes for valid ground and midifies the input transientPosition if ground snapping occurs
        /// </summary>
        public static void ProbeGround(this KinematicCharacterComponent self,
            ref float3 probingPosition, quaternion atRotation, float probingDistance, ref CharacterGroundingReport groundingReport)
        {
            if (probingDistance < KinematicCharacterComponent.MinimumGroundProbingDistance)
            {
                probingDistance = KinematicCharacterComponent.MinimumGroundProbingDistance;
            }

            int groundSweepsMade = 0;
            GameplayRaycastHit groundSweepHit = new GameplayRaycastHit();
            bool groundSweepingIsOver = false;
            float3 groundSweepPosition = probingPosition;
            float3 groundSweepDirection = math.mul(atRotation, -self._cachedWorldUp);
            float groundProbeDistanceRemaining = probingDistance;
            while (groundProbeDistanceRemaining > 0 && (groundSweepsMade <= KinematicCharacterComponent.MaxGroundingSweepIterations) && !groundSweepingIsOver)
            {
                // Sweep for ground detection
                if (self.CharacterGroundSweep(
                        groundSweepPosition, // position
                        atRotation, // rotation
                        groundSweepDirection, // direction
                        groundProbeDistanceRemaining, // distance
                        out groundSweepHit)) // hit
                {
                    float3 targetPosition = groundSweepPosition + (groundSweepDirection * groundSweepHit.distance);
                    HitStabilityReport groundHitStabilityReport = new HitStabilityReport();
                    self.EvaluateHitStability((GameplayCollider)groundSweepHit.Collider, groundSweepHit.normal, groundSweepHit.point, targetPosition, self._transientRotation, self._baseVelocity, ref groundHitStabilityReport);

                    groundingReport.FoundAnyGround = true;
                    groundingReport.GroundNormal = groundSweepHit.normal;
                    groundingReport.InnerGroundNormal = groundHitStabilityReport.InnerNormal;
                    groundingReport.OuterGroundNormal = groundHitStabilityReport.OuterNormal;
                    groundingReport.GroundCollider = (GameplayCollider)groundSweepHit.Collider;
                    groundingReport.GroundPoint = groundSweepHit.point;
                    groundingReport.SnappingPrevented = false;

                    // Found stable ground
                    if (groundHitStabilityReport.IsStable)
                    {
                        // Find all scenarios where ground snapping should be canceled
                        groundingReport.SnappingPrevented = !self.IsStableWithSpecialCases(ref groundHitStabilityReport, self._baseVelocity);

                        groundingReport.IsStableOnGround = true;

                        // Ground snapping
                        if (!groundingReport.SnappingPrevented)
                        {
                            probingPosition = groundSweepPosition + (groundSweepDirection * (groundSweepHit.distance - KinematicCharacterComponent.CollisionOffset));
                        }

                        self.CharacterMovementController.OnGroundHit((GameplayCollider)groundSweepHit.Collider, groundSweepHit.normal, groundSweepHit.point, ref groundHitStabilityReport);
                        groundSweepingIsOver = true;
                    }
                    else
                    {
                        // Calculate movement from this iteration and advance position
                        float3 sweepMovement = (groundSweepDirection * groundSweepHit.distance) + (math.mul(atRotation, self._cachedWorldUp) * math.max(KinematicCharacterComponent.CollisionOffset, groundSweepHit.distance));
                        groundSweepPosition = groundSweepPosition + sweepMovement;

                        // Set remaining distance
                        groundProbeDistanceRemaining = math.min(KinematicCharacterComponent.GroundProbeReboundDistance, math.max(groundProbeDistanceRemaining - math.length(sweepMovement), 0f));

                        // Reorient direction
                        groundSweepDirection = math.normalizesafe(mathEx.ProjectOnPlane(groundSweepDirection, groundSweepHit.normal));
                    }
                }
                else
                {
                    groundSweepingIsOver = true;
                }

                groundSweepsMade++;
            }
        }

        /// <summary>
        /// Forces the character to unground itself on its next grounding update
        /// </summary>
        public static void ForceUnground(this KinematicCharacterComponent self, float time = 0.1f)
        {
            self._mustUnground = true;
            self._mustUngroundTimeCounter = time;
        }
        public static bool MustUnground(this KinematicCharacterComponent self)
        {
            return self._mustUnground || self._mustUngroundTimeCounter > 0f;
        }

        /// <summary>
        /// Determines if the motor is considered stable on a given hit
        /// </summary>
        public static void EvaluateHitStability(this KinematicCharacterComponent self, 
            GameplayCollider hitCollider, float3 hitNormal, float3 hitPoint, float3 atCharacterPosition, quaternion atCharacterRotation, float3 withCharacterVelocity, ref HitStabilityReport stabilityReport)
        {
            if (!self._solveGrounding)
            {
                stabilityReport.IsStable = false;
                return;
            }
            
            float3 atCharacterUp = math.mul(atCharacterRotation, self._cachedWorldUp);
            float3 innerHitDirection = math.normalizesafe(mathEx.ProjectOnPlane(hitNormal, atCharacterUp));

            stabilityReport.IsStable = self.IsStableOnNormal(hitNormal);

            stabilityReport.FoundInnerNormal = false;
            stabilityReport.FoundOuterNormal = false;
            stabilityReport.InnerNormal = hitNormal;
            stabilityReport.OuterNormal = hitNormal;

            // Ledge handling
            if (self.LedgeAndDenivelationHandling)
            {
                float ledgeCheckHeight = KinematicCharacterComponent.MinDistanceForLedge;
                if (self.StepHandling != StepHandlingMethod.None)
                {
                    ledgeCheckHeight = self.MaxStepHeight;
                }

                bool isStableLedgeInner = false;
                bool isStableLedgeOuter = false;
                
                if(self.CharacterCollisionsRaycast(
                       hitPoint + (atCharacterUp * KinematicCharacterComponent.SecondaryProbesVertical) + (innerHitDirection * KinematicCharacterComponent.SecondaryProbesHorizontal),
                       -atCharacterUp,
                       ledgeCheckHeight + KinematicCharacterComponent.SecondaryProbesVertical,
                       out GameplayRaycastHit innerLedgeHit,
                       self._internalCharacterHits) > 0)
                {
                    float3 innerLedgeNormal = innerLedgeHit.normal;
                    stabilityReport.InnerNormal = innerLedgeNormal;
                    stabilityReport.FoundInnerNormal = true;
                    isStableLedgeInner = self.IsStableOnNormal(innerLedgeNormal);
                }
                
                if(self.CharacterCollisionsRaycast(
                       hitPoint + (atCharacterUp * KinematicCharacterComponent.SecondaryProbesVertical) + (-innerHitDirection * KinematicCharacterComponent.SecondaryProbesHorizontal),
                       -atCharacterUp,
                       ledgeCheckHeight + KinematicCharacterComponent.SecondaryProbesVertical,
                       out GameplayRaycastHit outerLedgeHit,
                       self._internalCharacterHits) > 0)
                {
                    float3 outerLedgeNormal = outerLedgeHit.normal;
                    stabilityReport.OuterNormal = outerLedgeNormal;
                    stabilityReport.FoundOuterNormal = true;
                    isStableLedgeOuter = self.IsStableOnNormal(outerLedgeNormal);
                }
                
                stabilityReport.LedgeDetected = (isStableLedgeInner != isStableLedgeOuter);
                if (stabilityReport.LedgeDetected)
                {
                    stabilityReport.IsOnEmptySideOfLedge = isStableLedgeOuter && !isStableLedgeInner;
                    stabilityReport.LedgeGroundNormal = isStableLedgeOuter ? stabilityReport.OuterNormal : stabilityReport.InnerNormal;
                    stabilityReport.LedgeRightDirection = math.normalizesafe(math.cross(hitNormal, stabilityReport.LedgeGroundNormal));
                    stabilityReport.LedgeFacingDirection = math.normalizesafe(mathEx.ProjectOnPlane(math.cross(stabilityReport.LedgeGroundNormal, stabilityReport.LedgeRightDirection), self._characterUp));
                    stabilityReport.DistanceFromLedge = math.length(mathEx.ProjectOnPlane(hitPoint - (atCharacterPosition + math.mul(atCharacterRotation, self.CharacterTransformToCapsuleBottom)), atCharacterUp));
                    stabilityReport.IsMovingTowardsEmptySideOfLedge = math.dot(math.normalizesafe(withCharacterVelocity), stabilityReport.LedgeFacingDirection) > 0f;
                }

                if (stabilityReport.IsStable)
                {
                    stabilityReport.IsStable = self.IsStableWithSpecialCases(ref stabilityReport, withCharacterVelocity);
                }
            }
            
            // Step handling
            if (self.StepHandling != StepHandlingMethod.None && !stabilityReport.IsStable)
            {
                // Stepping not supported on dynamic rigidbodies
                GameplayRigidbody hitRigidbody = hitCollider.AttachedRigidbody;
                if (!(hitRigidbody != null && !hitRigidbody.isKinematic))
                {
                    self.DetectSteps(atCharacterPosition, atCharacterRotation, hitPoint, innerHitDirection, ref stabilityReport);

                    if (stabilityReport.ValidStepDetected)
                    {
                        stabilityReport.IsStable = true;
                    }
                }
            }
            
            self.CharacterMovementController.ProcessHitStabilityReport(hitCollider, hitNormal, hitPoint, atCharacterPosition, atCharacterRotation, ref stabilityReport);
        }
        
        private static void DetectSteps(this KinematicCharacterComponent self, float3 characterPosition, quaternion characterRotation, float3 hitPoint, float3 innerHitDirection, ref HitStabilityReport stabilityReport)
        {
            int nbStepHits = 0;
            GameplayCollider tmpCollider;
            GameplayRaycastHit outerStepHit;
            float3 characterUp = math.mul(characterRotation, self._cachedWorldUp);
            float3 verticalCharToHit = math.project((hitPoint - characterPosition), characterUp);
            float3 horizontalCharToHitDirection = math.normalizesafe(mathEx.ProjectOnPlane((hitPoint - characterPosition), characterUp));
            float3 stepCheckStartPos = (hitPoint - verticalCharToHit) + (characterUp * self.MaxStepHeight) + (horizontalCharToHitDirection * KinematicCharacterComponent.CollisionOffset * 3f); 
            
            // Do outer step check with capsule cast on hit point
            nbStepHits = self.CharacterCollisionsSweep(
                stepCheckStartPos,
                characterRotation,
                -characterUp,
                self.MaxStepHeight + KinematicCharacterComponent.CollisionOffset,
                out outerStepHit,
                self._internalCharacterHits,
                0f,
                true);
            
            // Check for overlaps and obstructions at the hit position
            if (self.CheckStepValidity(nbStepHits, characterPosition, characterRotation, innerHitDirection, stepCheckStartPos, out tmpCollider))
            {
                stabilityReport.ValidStepDetected = true;
                stabilityReport.SteppedCollider = tmpCollider;
            }
            
            if (self.StepHandling == StepHandlingMethod.Extra && !stabilityReport.ValidStepDetected)
            {
                // Do min reach step check with capsule cast on hit point
                stepCheckStartPos = characterPosition + (characterUp * self.MaxStepHeight) + (-innerHitDirection * self.MinRequiredStepDepth);
                nbStepHits = self.CharacterCollisionsSweep(
                    stepCheckStartPos,
                    characterRotation,
                    -characterUp,
                    self.MaxStepHeight + KinematicCharacterComponent.CollisionOffset,
                    out outerStepHit,
                    self._internalCharacterHits,
                    0f,
                    true);

                // Check for overlaps and obstructions at the hit position
                if (self.CheckStepValidity(nbStepHits, characterPosition, characterRotation, innerHitDirection, stepCheckStartPos, out tmpCollider))
                {
                    stabilityReport.ValidStepDetected = true;
                    stabilityReport.SteppedCollider = tmpCollider;
                }
            }
        }

        private static bool CheckStepValidity(this KinematicCharacterComponent self, 
            int nbStepHits, float3 characterPosition, quaternion characterRotation, float3 innerHitDirection, float3 stepCheckStartPos, out GameplayCollider hitCollider)

        {
            hitCollider = null;
            float3 characterUp = math.mul(characterRotation, mathEx.Up);

            // Find the farthest valid hit for stepping
            bool foundValidStepPosition = false;
            
            while (nbStepHits > 0 && !foundValidStepPosition)
            {
                // Get farthest hit among the remaining hits
                GameplayRaycastHit farthestHit = new GameplayRaycastHit();
                float farthestDistance = 0f;
                int farthestIndex = 0;
                for (int i = 0; i < nbStepHits; i++)
                {
                    float hitDistance = self._internalCharacterHits[i].distance;
                    if (hitDistance > farthestDistance)
                    {
                        farthestDistance = hitDistance;
                        farthestHit = self._internalCharacterHits[i];
                        farthestIndex = i;
                    }
                }
                
                float3 characterPositionAtHit = stepCheckStartPos + (-characterUp * (farthestHit.distance - KinematicCharacterComponent.CollisionOffset));
                
                int atStepOverlaps = self.CharacterCollisionsOverlap(characterPositionAtHit, characterRotation, self._internalProbedColliders);
                if (atStepOverlaps <= 0)
                {
                    // Check for outer hit slope normal stability at the step position
                    if (self.CharacterCollisionsRaycast(
                            farthestHit.point + (characterUp * KinematicCharacterComponent.SecondaryProbesVertical) +
                            (-innerHitDirection * KinematicCharacterComponent.SecondaryProbesHorizontal),
                            -characterUp,
                            self.MaxStepHeight + KinematicCharacterComponent.SecondaryProbesVertical,
                            out GameplayRaycastHit outerSlopeHit,
                            self._internalCharacterHits,
                            true) > 0)
                    {
                        if (self.IsStableOnNormal(outerSlopeHit.normal))
                        {
                            // Cast upward to detect any obstructions to moving there
                            if (self.CharacterCollisionsSweep(
                                    characterPosition, // position
                                    characterRotation, // rotation
                                    characterUp, // direction
                                    self.MaxStepHeight - farthestHit.distance, // distance
                                    out GameplayRaycastHit tmpUpObstructionHit, // closest hit
                                    self._internalCharacterHits) // all hits
                                <= 0)
                            {
                                // Do inner step check...
                                bool innerStepValid = false;
                                GameplayRaycastHit innerStepHit;

                                if (self.AllowSteppingWithoutStableGrounding)
                                {
                                    innerStepValid = true;
                                }
                                else
                                {
                                    // At the capsule center at the step height
                                    if (self.CharacterCollisionsRaycast(
                                            characterPosition + math.project((characterPositionAtHit - characterPosition), characterUp),
                                            -characterUp,
                                            self.MaxStepHeight,
                                            out innerStepHit,
                                            self._internalCharacterHits,
                                            true) > 0)
                                    {
                                        if (self.IsStableOnNormal(innerStepHit.normal))
                                        {
                                            innerStepValid = true;
                                        }
                                    }
                                    
                                    if (!innerStepValid)
                                    {
                                        // At inner step of the step point
                                        if (self.CharacterCollisionsRaycast(
                                                farthestHit.point + (innerHitDirection * KinematicCharacterComponent.SecondaryProbesHorizontal),
                                                -characterUp,
                                                self.MaxStepHeight,
                                                out innerStepHit,
                                                self._internalCharacterHits,
                                                true) > 0)
                                        {
                                            if (self.IsStableOnNormal(innerStepHit.normal))
                                            {
                                                innerStepValid = true;
                                            }
                                        }
                                    }
                                    
                                    // Final validation of step
                                    if (innerStepValid)
                                    {
                                        hitCollider = (GameplayCollider)farthestHit.Collider;
                                        foundValidStepPosition = true;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Discard hit if not valid step
                if (!foundValidStepPosition)
                {
                    nbStepHits--;
                    if (farthestIndex < nbStepHits)
                    {
                        self._internalCharacterHits[farthestIndex] = self._internalCharacterHits[nbStepHits];
                    }
                }
            }

            return false;
        }
        
        /// <summary>
        /// Get true linear velocity (taking into account rotational velocity) on a given point of a rigidbody
        /// </summary>
        public static void GetVelocityFromRigidbodyMovement(this KinematicCharacterComponent self,
            GameplayRigidbody interactiveRigidbody, float3 atPoint, float deltaTime, out float3 linearVelocity, out float3 angularVelocity)
        {
            if (deltaTime > 0f)
            {
                linearVelocity = interactiveRigidbody.Velocity;
                angularVelocity = interactiveRigidbody.AngularVelocity;
                if(interactiveRigidbody.isKinematic)
                {
                    KinematicRigidbodyMover physicsMover = interactiveRigidbody.Parent.GetComponent<KinematicRigidbodyMover>();
                    if (physicsMover != null)
                    {
                        linearVelocity = physicsMover.Velocity;
                        angularVelocity = physicsMover.AngularVelocity;
                    }
                }

                if (!angularVelocity.Equals(float3.zero))
                {
                    var transform = interactiveRigidbody.GetParentTransform();
                    float3 centerOfRotation = transform.TransformPosition(interactiveRigidbody.CenterOfMass);

                    float3 centerOfRotationToPoint = atPoint - centerOfRotation;
                    quaternion rotationFromInteractiveRigidbody = quaternion.Euler(math.degrees(angularVelocity * deltaTime));
                    float3 finalPointPosition = centerOfRotation + math.mul(rotationFromInteractiveRigidbody, centerOfRotationToPoint);
                    linearVelocity += (finalPointPosition - atPoint) / deltaTime;
                }
            }
            else
            {
                linearVelocity = default;
                angularVelocity = default;
                return;
            }
        }

        #region Character Scene Queries
        
        /// <summary>
        /// Detect if the character capsule is overlapping with anything collidable
        /// </summary>
        /// <param name="self"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="overlappedColliders"></param>
        /// <param name="inflate"></param>
        /// <param name="acceptOnlyStableGroundLayer"></param>
        /// <returns></returns>
        public static int CharacterCollisionsOverlap(this KinematicCharacterComponent self, float3 position, quaternion rotation, GameplayCollider[] overlappedColliders, float inflate = 0f, bool acceptOnlyStableGroundLayer = false)
        {
            int queryLayers = self.CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = self.CollidableLayers & self.StableGroundLayers;
            }

            float3 bottom = position + math.mul(rotation, self.CharacterTransformToCapsuleBottomHemi);
            float3 top = position + math.mul(rotation, self.CharacterTransformToCapsuleTopHemi);
            if (inflate != 0f)
            {
                bottom += math.mul(rotation, new float3(0, -1, 0) * inflate);
                top += math.mul(rotation, new float3(0, 1, 0) * inflate);
            }

            int nbHits = 0;
            int nbUnfilteredHits = GameplayPhysics.OverlapCapsuleNonAlloc(self.Scene(),
                bottom,
                top, 
                self.CapsuleRadius + inflate,
                queryLayers,
                overlappedColliders);
            
            // Filter out invalid colliders
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                if (!self.CheckIfColliderValidForCollisions(overlappedColliders[i]))
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        overlappedColliders[i] = overlappedColliders[nbHits];
                    }
                }
            }

            return nbHits;
        }

        /// <summary>
        ///  Detect if the character capsule is overlapping with anything
        /// </summary>
        /// <param name="self"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="overlappedColliders"></param>
        /// <param name="layerMask"></param>
        /// <param name="inflate"></param>
        /// <returns></returns>
        public static int CharacterOverlap(this KinematicCharacterComponent self, float3 position, quaternion rotation, GameplayCollider[] overlappedColliders, int layerMask, float inflate = 0f)
        {
            float3 bottom = position + math.mul(rotation, self.CharacterTransformToCapsuleBottomHemi);
            float3 top = position + math.mul(rotation, self.CharacterTransformToCapsuleTopHemi);
            if (inflate != 0f)
            {
                bottom += math.mul(rotation, new float3(0, -1, 0) * inflate);
                top += math.mul(rotation, new float3(0, 1, 0) * inflate);
            }
            
            int nbHits = 0;
            int nbUnfilteredHits = GameplayPhysics.OverlapCapsuleNonAlloc(self.Scene(),
                bottom,
                top, 
                self.CapsuleRadius + inflate,
                layerMask,
                overlappedColliders);
            
            // Filter out the character capsule itself
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                if (overlappedColliders[i] == self.Capsule)
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        overlappedColliders[i] = overlappedColliders[nbHits];
                    }
                }
            }

            return nbHits;
        }
        
        /// <summary>
        /// Sweeps the capsule's volume to detect collision hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public static int CharacterCollisionsSweep(this KinematicCharacterComponent self, float3 position, quaternion rotation, float3 direction, float distance, out GameplayRaycastHit closestHit, GameplayRaycastHit[] hits, float inflate = 0f, bool acceptOnlyStableGroundLayer = false)
        {
            int queryLayers = self.CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = self.CollidableLayers & self.StableGroundLayers;
            }

            float3 bottom = position + math.mul(rotation, self.CharacterTransformToCapsuleBottomHemi) - (direction * KinematicCharacterComponent.SweepProbingBackstepDistance);
            float3 top = position + math.mul(rotation, self.CharacterTransformToCapsuleTopHemi) - (direction * KinematicCharacterComponent.SweepProbingBackstepDistance);
            if (inflate != 0f)
            {
                bottom += math.mul(rotation, new float3(0, -1, 0) * inflate);
                top += math.mul(rotation, new float3(0, 1, 0) * inflate);
            }

            // Capsule cast
            int nbHits = 0;
            int nbUnfilteredHits = GameplayPhysics.CapsuleCastNonAlloc(self.Scene(),
                    bottom,
                    top,
                    self.CapsuleRadius + inflate,
                    direction,
                    distance + KinematicCharacterComponent.SweepProbingBackstepDistance,
                    queryLayers,
                    hits);

            // Hits filter
            closestHit = new GameplayRaycastHit();
            float closestDistance = float.PositiveInfinity;
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                hits[i].distance -= KinematicCharacterComponent.SweepProbingBackstepDistance;

                GameplayRaycastHit hit = hits[i];
                float hitDistance = hit.distance;

                // Filter out the invalid hits
                if (hitDistance <= 0f || !self.CheckIfColliderValidForCollisions((GameplayCollider)hit.Collider))
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        hits[i] = hits[nbHits];
                    }
                }
                else
                {
                    // Remember closest valid hit
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hitDistance;
                    }
                }
            }

            return nbHits;
        }
        
                /// <summary>
        /// Sweeps the capsule's volume to detect hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public static int CharacterSweep(this KinematicCharacterComponent self, float3 position, quaternion rotation, float3 direction, float distance, out GameplayRaycastHit closestHit, GameplayRaycastHit[] hits, int layers,  float inflate = 0f)
        {
            closestHit = new GameplayRaycastHit();

            float3 bottom = position + math.mul(rotation, self.CharacterTransformToCapsuleBottomHemi);
            float3 top = position + math.mul(rotation, self.CharacterTransformToCapsuleTopHemi);
            if (inflate != 0f)
            {
                bottom += math.mul(rotation, new float3(0, -1, 0) * inflate);
                top += math.mul(rotation, new float3(0, 1, 0) * inflate);
            }

            // Capsule cast
            int nbHits = 0;
            int nbUnfilteredHits = GameplayPhysics.CapsuleCastNonAlloc(self.Scene(),
                bottom,
                top,
                self.CapsuleRadius + inflate,
                direction,
                distance,
                layers,
                hits);

            // Hits filter
            float closestDistance = float.PositiveInfinity;
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                GameplayRaycastHit hit = hits[i];

                // Filter out the character capsule
                if (hit.distance <= 0f || (GameplayCollider)hit.Collider == self.Capsule)
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        hits[i] = hits[nbHits];
                    }
                }
                else
                {
                    // Remember closest valid hit
                    float hitDistance = hit.distance;
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hitDistance;
                    }
                }
            }

            return nbHits;
        }
                
        /// <summary>
        /// Casts the character volume in the character's downward direction to detect ground
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        private static bool CharacterGroundSweep(this KinematicCharacterComponent self, float3 position, quaternion rotation, float3 direction, float distance, out GameplayRaycastHit closestHit)
        {
            int queryLayers = self.CollidableLayers & self.StableGroundLayers;

            
            closestHit = new GameplayRaycastHit();

            float3 bottom = position + math.mul(rotation, self.CharacterTransformToCapsuleBottomHemi) - (direction * KinematicCharacterComponent.GroundProbingBackstepDistance);
            float3 top = position + math.mul(rotation, self.CharacterTransformToCapsuleTopHemi) - (direction * KinematicCharacterComponent.GroundProbingBackstepDistance);

            // Capsule cast
            int nbUnfilteredHits = GameplayPhysics.CapsuleCastNonAlloc(self.Scene(),
                bottom,
                top,
                self.CapsuleRadius,
                direction,
                distance + KinematicCharacterComponent.GroundProbingBackstepDistance,
                queryLayers,
                self._internalCharacterHits);

            // Hits filter
            bool foundValidHit = false;
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < nbUnfilteredHits; i++)
            {
                GameplayRaycastHit hit = self._internalCharacterHits[i];
                float hitDistance = hit.distance;

                // Find the closest valid hit
                if (hitDistance > 0f && self.CheckIfColliderValidForCollisions((GameplayCollider)hit.Collider))
                {
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestHit.distance -= KinematicCharacterComponent.GroundProbingBackstepDistance;
                        closestDistance = hitDistance;

                        foundValidHit = true;
                    }
                }
            }

            return foundValidHit;
        }
        
        /// <summary>
        /// Raycasts to detect collision hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public static int CharacterCollisionsRaycast(this KinematicCharacterComponent self, float3 position, float3 direction, float distance, out GameplayRaycastHit closestHit, GameplayRaycastHit[] hits, bool acceptOnlyStableGroundLayer = false)
        {
            int queryLayers = self.CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = self.CollidableLayers & self.StableGroundLayers;
            }

            // Raycast
            int nbHits = 0;
            int nbUnfilteredHits = GameplayPhysics.RaycastNonAlloc(self.Scene(),
                position,
                direction,
                distance,
                queryLayers,
                hits);

            // Hits filter
            closestHit = new GameplayRaycastHit();
            float closestDistance = float.PositiveInfinity;
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                GameplayRaycastHit hit = hits[i];
                float hitDistance = hit.distance;

                // Filter out the invalid hits
                if (hitDistance <= 0f || !self.CheckIfColliderValidForCollisions((GameplayCollider)hit.Collider))
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        hits[i] = hits[nbHits];
                    }
                }
                else
                {
                    // Remember closest valid hit
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hitDistance;
                    }
                }
            }

            return nbHits;
        }
        
        
        /// <summary>
        /// Determines if the input collider is valid for collision processing
        /// </summary>
        /// <returns> Returns true if the collider is valid </returns>
        private static bool CheckIfColliderValidForCollisions(this KinematicCharacterComponent self, GameplayCollider coll)
        {
            // Ignore self
            if (coll == self.Capsule)
            {
                return false;
            }

            if (!self.InternalIsColliderValidForCollisions(coll))
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Determines if the input collider is valid for collision processing
        /// </summary>
        private static bool InternalIsColliderValidForCollisions(this KinematicCharacterComponent self, GameplayCollider coll)
        {
            GameplayRigidbody colliderAttachedRigidbody = coll.AttachedRigidbody;
            if (colliderAttachedRigidbody != null)
            {
                bool isRigidbodyKinematic = colliderAttachedRigidbody.isKinematic;

                // If movement is made from AttachedRigidbody, ignore the AttachedRigidbody
                if (self._isMovingFromAttachedRigidbody && (!isRigidbodyKinematic || colliderAttachedRigidbody == self._attachedRigidbody))
                {
                    return false;
                }

                // don't collide with dynamic rigidbodies if our RigidbodyInteractionType is kinematic
                if (self.RigidbodyInteractionType == RigidbodyInteractionType.Kinematic && !isRigidbodyKinematic)
                {
                    // wake up rigidbody
                    if (coll.AttachedRigidbody != null)
                    {
                        coll.AttachedRigidbody.WakeUp();
                    }

                    return false;
                }
            }

            // Custom checks
            bool colliderValid = self.CharacterMovementController.IsColliderValidForCollisions(coll);
            if (!colliderValid)
            {
                return false;
            }

            return true;
        }
        #endregion
        
    }
}