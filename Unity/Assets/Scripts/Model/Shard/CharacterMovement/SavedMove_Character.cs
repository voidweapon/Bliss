using Bliss.Mathematics;
using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    public class SavedMove_Character
    {
        public bool bForceNoCombine;
        public bool bOldTimeStampBeforeReset;
            
        public float TimeStamp;
        public float DeltaTime;
        public float CustomTimeDilation;

        public float JumpKeyHoldTime;
        public float JumpForceTimeRemaining;
        public int JumpMaxCount;
        public int JumpCurrentCount;

        #region Information at the start of the move
        public KinematicCharacterMotorState StartCCState;
        public byte StartPackedMovementMode;
        public float3 StartLocation;
        public float3 StartRelativeLocation;
        public quaternion StartRotation;
        public quaternion StartControlRotation;
        public float3 StartVelocity;
        public float3 StartAcceleration;
        public float3 StartAngularVelocity;
        public float StartCapsuleRadius;
        public float StartCapsuleHeight;
        public uint StartActorOverlapEventCounter;
        public uint StartComponentOverlapEventCounter;
        public EntityRef<Entity> StartBase;
        public string StartBoneName;
        public quaternion StartBaseRotation;
        public EntityRef<Entity> StartAttachParent;
        public string  StartAttachSocketName;
        public float3  StartAttachRelativeLocation;
        public quaternion StartAttachRelativeRotation;
        #endregion

        #region Information after the move has been performed
        public KinematicCharacterMotorState EndCCState;
        public byte EndPackedMovementMode;
        public float3 EndLocation;
        public quaternion EndRotation;
        public float3 EndVelocity;
        public float3 EndAcceleration;
        public float3 EndAngularVelocity;
        public float EndCapsuleRadius;
        public float EndCapsuleHalfHeight;
        public uint EndActorOverlapEventCounter;
        public uint EndComponentOverlapEventCounter;
        public EntityRef<Entity> EndBase;
        public string EndBoneName;
        public quaternion EndBaseRotation;
        public EntityRef<Entity> EndAttachParent;
        public string  EndAttachSocketName;
        public float3  EndAttachRelativeLocation;
        public quaternion EndAttachRelativeRotation;
        #endregion
        
        
        public float3 Acceleration;
        public float MaxSpeed;

        // Cached to speed up iteration over IsImportantMove().
        public float3 AccelNormal;
        public float AccelMag;

        /** Threshold for deciding this is an "important" move based on DP with last acked acceleration. */
        public const float AccelDotThreshold = 0.1f;
        /** Threshold for deciding is this is an important move because acceleration magnitude has changed too much */
        public const float AccelMagThreshold = 0.9f;
        /** Threshold for deciding if we can combine two moves, true if cosine of angle between them is <= this. */
        public const float AccelDotThresholdCombine = 0.996f;
        /** Client saved moves will not combine if the result of GetMaxSpeed() differs by this much between moves. */
        public const float MaxSpeedThresholdCombine = 10f;

        public void Clear()
        {
            bForceNoCombine = false;
            bOldTimeStampBeforeReset = false;
            
            TimeStamp = 0f;
            DeltaTime = 0f;
            CustomTimeDilation = 1f;

            StartPackedMovementMode = 0;
            StartLocation = float3.zero;
            StartRelativeLocation = float3.zero;
            StartRotation = quaternion.identity;
            StartControlRotation = quaternion.identity;
            StartVelocity = float3.zero;
            StartAcceleration = float3.zero;
            StartAngularVelocity = float3.zero;
            StartCapsuleRadius = 0f;
            StartCapsuleHeight = 0f;
            StartActorOverlapEventCounter = 0;
            StartComponentOverlapEventCounter = 0;
            StartBase = default(EntityRef<Entity>);
            StartBoneName = string.Empty;
            StartAttachParent = default(EntityRef<Entity>);
            StartAttachSocketName = string.Empty;
            StartAttachRelativeLocation = float3.zero;
            StartAttachRelativeRotation = quaternion.identity;

            EndPackedMovementMode = 0;
            EndLocation = float3.zero;
            EndRotation = quaternion.identity;
            EndVelocity = float3.zero;
            EndAcceleration = float3.zero;
            EndAngularVelocity = float3.zero;
            EndCapsuleRadius = 0f;
            EndCapsuleHalfHeight = 0f;
            EndActorOverlapEventCounter = 0;
            EndComponentOverlapEventCounter = 0;
            EndBase = default(EntityRef<Entity>);
            EndBoneName = string.Empty;
            EndAttachParent = default(EntityRef<Entity>);
            EndAttachSocketName = string.Empty;
            EndAttachRelativeLocation = float3.zero;
            EndAttachRelativeRotation = quaternion.identity;
            
            Acceleration = float3.zero;
            MaxSpeed = 0f;
            AccelNormal = float3.zero;
            AccelMag = 0f;
        }
        
        public void SetMoveFor(CharacterMovementComponent mover,
            byte packNetworkMovementMode,
            float maxSpeed,
            float inDeltaTime,
            float3 newAccel,
            float3 roundAccel,
            NetworkPredictionData_Client_CharacterMovement clientData)
        {
            StartPackedMovementMode = packNetworkMovementMode;
            MaxSpeed =  maxSpeed;
            DeltaTime = inDeltaTime;

            AccelMag = math.length(newAccel);
            Acceleration = AccelMag > mathEx.KINDA_SMALL_NUMBER ? newAccel / AccelMag : float3.zero;
            Acceleration = roundAccel;
            
            TimeStamp = clientData.CurrentTimeStamp;
            
            // Root motion source-containing moves should never be combined


            // Moves with anim root motion should not be combined

            
            // Launch velocity gives instant and potentially huge change of velocity
            // Avoid combining move to prevent from reverting locations until server catches up
        }


        public void SetInitialPosition(CharacterMovementComponent mover)
        {
            var transform = mover.TransformCmp.Transform;
            StartLocation = transform.Location;
            StartRotation = transform.Rotation;
            StartVelocity = mover.KinematicCharacterCmp.Velocity;

            //TODO: for test
            {
                CustomTimeDilation = 1f;
                StartControlRotation = StartRotation;
                StartActorOverlapEventCounter = 0;
                StartComponentOverlapEventCounter = 0;
            }
            
            StartCapsuleRadius =  mover.KinematicCharacterCmp.CapsuleRadius;
            StartCapsuleHeight = mover.KinematicCharacterCmp.CapsuleHeight;
            
            // Jump State
            
            // Base State
            
            // Attach State
            
        }

        public void PostUpdate(CharacterMovementComponent mover,
            byte packNetworkMovementMode)
        {
            EndPackedMovementMode = packNetworkMovementMode;
            
            var transform = mover.TransformCmp.Transform;
            EndLocation = transform.Location;
            EndRotation = transform.Rotation;
            EndVelocity = mover.KinematicCharacterCmp.Velocity;
            
            // Movement base and base-relative movement
            
            
            // Attachment state

            
            // Don't want to combine moves that trigger overlaps, because by moving back and replaying the move we could retrigger overlaps.
            if ((StartActorOverlapEventCounter != EndActorOverlapEventCounter) || (StartComponentOverlapEventCounter != EndComponentOverlapEventCounter))
            {
                bForceNoCombine = true;
            }

            // Don't combine or delay moves where velocity changes to/from zero.
            if (StartVelocity.IsZero() != EndVelocity.IsZero())
            {
                bForceNoCombine = true;
            }
            
            // Don't combine if this move caused us to change movement modes during the move.
            if (StartPackedMovementMode != EndPackedMovementMode)
            {
                bForceNoCombine = true;
            }
            
            // Don't combine when jump input just began or ended during the move.
            
        }

        public bool IsImportantMove(SavedMove_Character lastAckedMove)
        {
            // Check if any important movement flags have changed status.
            if (GetCompressedFlags() != lastAckedMove.GetCompressedFlags())
            {
                return true;
            }
            
            if (StartPackedMovementMode != lastAckedMove.EndPackedMovementMode)
            {
                return true;
            }

            if (EndPackedMovementMode != lastAckedMove.EndPackedMovementMode)
            {
                return true;
            }
            
            // check if acceleration has changed significantly
            if (!Acceleration.Equals(lastAckedMove.Acceleration))
            {
                // Compare magnitude and orientation
                if (math.abs(AccelMag - lastAckedMove.AccelMag) > AccelMagThreshold
                    || math.dot(AccelNormal, lastAckedMove.AccelNormal) > AccelDotThreshold)
                {
                    return true;
                }
            }
            
            return false;
        }

        public bool CanCombineWith(SavedMove_Character newMove, CharacterMovementComponent mover, float maxDelta)
        {
            if (bForceNoCombine || newMove.bForceNoCombine)
            {
                return false;
            }

            if (bOldTimeStampBeforeReset)
            {
                return false;
            }

            if (newMove.Acceleration.IsZero())
            {
                if (!Acceleration.IsZero())
                {
                    return false;
                }
            }
            else
            {
                if (newMove.DeltaTime + DeltaTime >= maxDelta)
                {
                    return false;
                }

                if (!Acceleration.Coincident(newMove.Acceleration, AccelDotThresholdCombine))
                {
                    return false;
                }
            }
            
            // Don't combine moves where velocity changes to zero or from zero.
            if (StartVelocity.IsZero() != newMove.StartVelocity.IsZero())
            {
                return false;
            }

            if (!MaxSpeed.IsNearlyEqual(newMove.MaxSpeed, MaxSpeedThresholdCombine))
            {
                return false;
            }

            if ((MaxSpeed == 0) != (newMove.MaxSpeed == 0))
            {
                return false;
            }
            
            //Jump State
            if ((JumpKeyHoldTime == 0) != (newMove.JumpKeyHoldTime == 0))
            {
                return false;
            }

            if ((JumpCurrentCount != newMove.JumpCurrentCount) || (JumpMaxCount != newMove.JumpMaxCount))
            {
                return false;
            }

            // Don't combine on changes to/from zero.
            if ((JumpForceTimeRemaining == 0) != (newMove.JumpForceTimeRemaining == 0))
            {
                return false;
            }

            if (GetCompressedFlags() != newMove.GetCompressedFlags())
            {
                return false;
            }

            if (StartPackedMovementMode != newMove.StartPackedMovementMode)
            {
                return false;
            }
            
            if (EndPackedMovementMode != newMove.StartPackedMovementMode)
            {
                return false;
            }
            
            if (StartCapsuleRadius != newMove.StartCapsuleRadius)
            {
                return false;
            }

            if (StartCapsuleHeight != newMove.StartCapsuleHeight)
            {
                return false;
            }

            if (CustomTimeDilation != newMove.CustomTimeDilation)
            {
                return false;
            }

            // Don't combine moves with overlap event changes, since reverting back and then moving forward again can cause event spam.
            if (EndActorOverlapEventCounter != newMove.StartActorOverlapEventCounter)
            {
                return false;
            }
            
            // Base
            Entity oldBase = (Entity)StartBase;
            Entity newBase = (Entity)newMove.StartBase;
            if (oldBase != newBase)
            {
                return false;
            }
            
            // No combining if attach parent changed.
            Entity oldStartAttach = (Entity)StartAttachParent;
            Entity oldEndAttach = (Entity)EndAttachParent;
            Entity newStartAttach = (Entity)newMove.StartAttachParent;
            if (oldStartAttach != newStartAttach || oldEndAttach != newStartAttach)
            {
                return false;
            }
            // No combining if attach socket changed.
            if (StartAttachSocketName != newMove.StartAttachSocketName || EndAttachSocketName != newMove.StartAttachSocketName)
            {
                return false;
            }

            if (newStartAttach != null)
            {
                // If attached, no combining if relative location changed.

                // For rotation, Yaw doesn't matter for capsules

            }
            else
            {
                // Not attached to anything. Only combine if base hasn't rotated.
                if(StartBaseRotation.Equals(newMove.StartBaseRotation) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public void CombineWith(SavedMove_Character oldMove, CharacterMovementComponent mover, float3 oldStartLocation)
        {
            // to combine move, first revert pawn position to PendingMove start position, before playing combined move on client

            
            // Now that we have reverted to the old position, prepare a new move from that position,
            // using our current velocity, acceleration, and rotation, but applied over the combined time from the old and new move.

            // Combine times for both moves
            DeltaTime += oldMove.DeltaTime;
            
            
            // Roll back jump force counters. SetInitialPosition() below will copy them to the saved move.
            JumpKeyHoldTime = oldMove.JumpKeyHoldTime;
            JumpCurrentCount = oldMove.JumpCurrentCount;
        }
        

        public byte GetCompressedFlags()
        {
            byte result = 0;
            
            return result;
        }
    }
}