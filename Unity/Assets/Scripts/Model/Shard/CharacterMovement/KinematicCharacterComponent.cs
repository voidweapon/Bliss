using System.Collections.Generic;
using Bliss.Mathematics;
using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    /// <summary>Stores the state of a character motor owned by a GameplayWorld.</summary>
    public class KinematicCharacterComponent : GamePlayEntity, IAwake, IDestroy
    {
        /// <summary>Instance identifier used to unregister the motor during destruction.</summary>
        public long RegisteredInstanceId;

        #region Capsule Setting
        /// <summary>
        /// The height is the actual height including the half-spheres at each end.
        /// </summary>
        public float CapsuleRadius = 0.5f;
        public float CapsuleHeight = 2f;
        public float CapsuleYOffset = 1f;
        #endregion 

        // Character Transform Info
        public float3 CharacterTransformToCapsuleCenter;
        public float3 CharacterTransformToCapsuleBottom;
        public float3 CharacterTransformToCapsuleTop;
        public float3 CharacterTransformToCapsuleBottomHemi;
        public float3 CharacterTransformToCapsuleTopHemi;

        
        public float3 CharacterUp => _characterUp;
        public float3 CharacterForward => _characterForward;
        public float3 CharacterRight => _characterRight;
        
        public float3 _characterUp = new float3(0, 1, 0);
        public float3 _characterForward = new float3(0, 0, 1);
        public float3 _characterRight = new float3(1, 0, 0);


        #region Grounding settings
        /// <summary>
        /// Increases the range of ground detection, to allow snapping to ground at very high speeds
        /// </summary>   
        public float GroundDetectionExtraDistance = 0f;
        /// <summary>
        /// Maximum slope angle on which the character can be stable
        /// </summary>   
        public float MaxStableSlopeAngle = 60f;
        /// <summary>
        /// Which layers can the character be considered stable on
        /// </summary>
        public int StableGroundLayers = GameplayLayerMask.WalkableMask;
        /// <summary>
        /// Notifies the Character Controller when discrete collisions are detected
        /// </summary>    
        public bool DiscreteCollisionEvents = false;
        #endregion

        #region Step settings
        /// <summary>
        /// Handles properly detecting grounding status on steps, but has a performance cost.
        /// </summary>
        public StepHandlingMethod StepHandling = StepHandlingMethod.None;
        /// <summary>
        /// Maximum height of a step which the character can climb
        /// </summary>
        public float MaxStepHeight = 0.5f;
        /// <summary>
        /// Can the character step up obstacles even if it is not currently stable?
        /// </summary>
        public bool AllowSteppingWithoutStableGrounding = false;
        /// <summary>
        /// Minimum length of a step that the character can step on (used in Extra stepping method. Use this to let the character step on steps that are smaller that its radius
        /// </summary>
        public float MinRequiredStepDepth = 0.1f;
        #endregion

        #region Ledge settings
        /// <summary>
        /// Handles properly detecting ledge information and grounding status, but has a performance cost.
        /// </summary>
        public bool LedgeAndDenivelationHandling = true;
        /// <summary>
        /// The distance from the capsule central axis at which the character can stand on a ledge and still be stable
        /// </summary>    
        public float MaxStableDistanceFromLedge = 0.5f;
        /// <summary>
        /// Prevents snapping to ground on ledges beyond a certain velocity
        /// </summary>    
        public float MaxVelocityForLedgeSnap = 0f;
        /// <summary>
        /// The maximun downward slope angle change that the character can be subjected to and still be snapping to the ground
        /// </summary>   
        public float MaxStableDenivelationAngle = 180f;
        #endregion

        #region Rigidbody interaction settings
        /// <summary>
        /// Handles properly being pushed by and standing on PhysicsMovers or dynamic rigidbodies. Also handles pushing dynamic rigidbodies
        /// </summary>
        public bool InteractiveRigidbodyHandling = true;
        /// <summary>
        /// How the character interacts with non-kinematic rigidbodies. \"Kinematic\" mode means the character pushes the rigidbodies with infinite force (as a kinematic body would). \"SimulatedDynamic\" pushes the rigidbodies with a simulated mass value.
        /// </summary>
        public RigidbodyInteractionType RigidbodyInteractionType;
        /// <summary>
        /// How the character interacts with non-kinematic rigidbodies. \"Kinematic\" mode means the character pushes the rigidbodies with infinite force (as a kinematic body would). \"SimulatedDynamic\" pushes the rigidbodies with a simulated mass value.
        /// </summary>
        public float SimulatedCharacterMass = 1f;
        /// <summary>
        /// Determines if the character preserves moving platform velocities when de-grounding from them
        /// </summary>
        public bool PreserveAttachedRigidbodyMomentum = true;
        #endregion
        

        public GameplayRigidbody AttachedRigidbodyOverride;
        
        /// <summary>
        /// Determines if the character's movement uses the planar constraint
        /// </summary>
        public bool HasPlanarConstraint = false;

        /// <summary>
        /// Defines the plane that the character's movement is constrained on, if <see cref="HasPlanarConstraint"/> is active
        /// </summary>
        public float3 PlanarConstraintAxis = new float3(0, 0, 1);

        
        /// <summary>
        /// How many times can we sweep for movement per update
        /// </summary>
        public int MaxMovementIterations = 5;
        
        /// <summary>
        /// How many times can we check for decollision per update
        /// </summary>
        public int MaxDecollisionIterations = 1;
        
        /// <summary>
        /// Checks for overlaps before casting movement, making sure all collisions are detected even when already intersecting geometry (has a performance cost, but provides safety against tunneling through colliders)
        /// </summary>
        public bool CheckMovementInitialOverlaps = true;
        
        public bool KillVelocityWhenExceedMaxMovementIterations = true;
        
        public bool KillRemainingMovementWhenExceedMaxMovementIterations = true;
        
        public bool LastMovementIterationFoundAnyGround;
        
        /// <summary>
        ///  Remembers initial position before all simulation are done
        /// </summary>
        public float3 InitialTickPosition;
        /// <summary>
        /// Remembers initial rotation before all simulation are done
        /// </summary>
        public quaternion InitialTickRotation;

        /// <summary>
        ///  The character's goal position in its movement calculations (always up-to-date during the character update phase)
        /// </summary>
        public float3 TransientPosition => _transientPosition;
        public float3 _transientPosition;

        /// <summary>
        ///  The character's goal rotation in its movement calculations (always up-to-date during the character update phase)
        /// </summary>
        public quaternion TransientRotation
        {
            get => _transientRotation;
            set
            {
                _transientRotation = value;
                if (_transientRotation.IsNaN())
                {
                    System.Diagnostics.Debugger.Break();
                }
                _characterUp = math.mul(_transientRotation, _characterUp);
                _characterForward = math.mul(_transientRotation, _cachedWorldForward);
                _characterRight = math.mul(_transientRotation, _cachedWorldRight);
            }
        }

        public quaternion _transientRotation;
        
        /// <summary>
        /// The character's position before the movement calculations began
        /// </summary>
        public float3 InitialSimulationPosition;
        /// <summary>
        /// The character's rotation before the movement calculations began
        /// </summary>
        public quaternion InitialSimulationRotation;

        // Velocity
        
        /// <summary>
        /// The character's total velocity, including velocity from standing on rigidbodies or PhysicsMover
        /// </summary>
        public float3 Velocity => BaseVelocity + AttachedRigidbodyVelocity;
        
        /// <summary>
        /// The character's velocity resulting from direct movement
        /// </summary>
        public float3 BaseVelocity => _baseVelocity;
        public float3 _baseVelocity;
        
        /// <summary>
        /// The character's velocity resulting from standing on rigidbodies or PhysicsMover
        /// </summary>
        public float3 AttachedRigidbodyVelocity => _attachedRigidbodyVelocity;
        public float3 _attachedRigidbodyVelocity;
        
        /// <summary>
        /// Contains the current grounding information
        /// </summary>
        public CharacterGroundingReport GroundingStatus = new CharacterGroundingReport();
        /// <summary>
        /// Contains the previous grounding information
        /// </summary>
        public CharacterTransientGroundingReport LastGroundingStatus = new CharacterTransientGroundingReport();
        
        // Collision
        /// <summary>
        /// Specifies the LayerMask that the character's movement algorithm can detect collisions with. By default, this uses the rigidbody's layer's collision matrix
        /// </summary>
        public int CollidableLayers = -1;
        
        /// <summary>
        /// The number of overlaps detected so far during character update (is reset at the beginning of the update)
        /// </summary>
        public int OverlapsCount { get { return _overlapsCount; } }
        /// <summary>
        /// The overlaps detected so far during character update
        /// </summary>
        public OverlapResult[] Overlaps => _overlaps;
        
        public ICharacterMovementController  CharacterMovementController;
        
        
        //--------------------------------------------------------------------
        //Private

        public GameplayRaycastHit[] _internalCharacterHits = new GameplayRaycastHit[MaxHitsBudget];
        public GameplayCollider[] _internalProbedColliders = new GameplayCollider[MaxCollisionBudget];
        public List<GameplayRigidbody>  _rigidbodiesPushedThisMove = new List<GameplayRigidbody>(16);
        public GameplayRigidbody _lastAttachedRigidbody;
        
        public bool _solveMovementCollisions = true;
        public bool _solveGrounding = true;
        
        public bool _movePositionDirty = false;
        public float3 _movePositionTarget = float3.zero;
        public bool _moveRotationDirty = false;
        public quaternion _moveRotationTarget = quaternion.identity;
        
        public bool _lastSolvedOverlapNormalDirty = false;
        private float3 _lastSolvedOverlapNormal = new float3(0, 0, 1);
        public int _overlapsCount;
        public OverlapResult[] _overlaps = new OverlapResult[MaxRigidbodyOverlapsCount];

        public RigidbodyProjectionHit[] _internalRigidbodyProjectionHits = new RigidbodyProjectionHit[MaxRigidbodyOverlapsCount];
        public int _rigidbodyProjectionHitCount = 0;
        public bool _isMovingFromAttachedRigidbody = false;
        
        public bool _mustUnground = false;
        public float _mustUngroundTimeCounter = 0f;

        
        
        // World Coordinate
        public float3 _cachedWorldUp = new float3(0, 1, 0);
        public float3 _cachedWorldForward = new float3(0, 0, 1);
        public float3 _cachedWorldRight = new float3(1, 0, 0);
        public float3 _cachedZeroVector = float3.zero;
        

        
        public GameplayCapsuleCollider Capsule;
        public TransformComponent TransformComponent;
        public SmoothTransformComponent SmoothTransformComponent;
        
        /// <summary>
        /// Represents the Rigidbody to stay attached to
        /// </summary>
        public GameplayRigidbody AttachedRigidbody => _attachedRigidbody;
        public GameplayRigidbody _attachedRigidbody;
        
        public const int MaxHitsBudget = 16;
        public const int MaxCollisionBudget = 16;
        public const int MaxGroundingSweepIterations = 2;
        public const int MaxSteppingSweepIterations = 3;
        public const int MaxRigidbodyOverlapsCount = 16;
        public const float CollisionOffset = 0.01f;
        public const float GroundProbeReboundDistance = 0.02f;
        public const float MinimumGroundProbingDistance = 0.005f;
        public const float GroundProbingBackstepDistance = 0.1f;
        public const float SweepProbingBackstepDistance = 0.002f;
        public const float SecondaryProbesVertical = 0.02f;
        public const float SecondaryProbesHorizontal = 0.001f;
        public const float MinVelocityMagnitude = 0.01f;
        public const float SteppingForwardDistance = 0.03f;
        public const float MinDistanceForLedge = 0.05f;
        public const float CorrelationForVerticalObstruction = 0.01f;
        public const float ExtraSteppingForwardDistance = 0.01f;
        public const float ExtraStepHeightPadding = 0.01f;
        
    }
    
    public enum RigidbodyInteractionType
    {
        None,
        Kinematic,
        SimulatedDynamic
    }

    public enum StepHandlingMethod
    {
        None,
        Standard,
        Extra
    }

    public enum MovementSweepState
    {
        Initial,
        AfterFirstHit,
        FoundBlockingCrease,
        FoundBlockingCorner,
    }
    
    /// <summary>
    /// Represents the entire state of a character motor that is pertinent for simulation.
    /// Use this to save state or revert to past state
    /// </summary>
    [System.Serializable]
    public struct KinematicCharacterMotorState
    {
        public float3 Position;
        public quaternion Rotation;
        public float3 BaseVelocity;

        public bool MustUnground;
        public float MustUngroundTime;
        public bool LastMovementIterationFoundAnyGround;
        public CharacterTransientGroundingReport GroundingStatus;

        public GameplayRigidbody AttachedRigidbody;
        public float3 AttachedRigidbodyVelocity;
    }

    /// <summary>
    /// Describes an overlap between the character capsule and another collider
    /// </summary>
    public struct OverlapResult
    {
        public float3 Normal;
        public GameplayCollider Collider;

        public OverlapResult(float3 normal, GameplayCollider collider)
        {
            Normal = normal;
            Collider = collider;
        }
    }
    
    /// <summary>
    /// Contains all the information for the motor's grounding status
    /// </summary>
    public struct CharacterGroundingReport
    {
        public bool FoundAnyGround;
        public bool IsStableOnGround;
        public bool SnappingPrevented;
        public float3 GroundNormal;
        public float3 InnerGroundNormal;
        public float3 OuterGroundNormal;

        public GameplayCollider GroundCollider;
        public float3 GroundPoint;

        public void CopyFrom(CharacterTransientGroundingReport transientGroundingReport)
        {
            FoundAnyGround = transientGroundingReport.FoundAnyGround;
            IsStableOnGround = transientGroundingReport.IsStableOnGround;
            SnappingPrevented = transientGroundingReport.SnappingPrevented;
            GroundNormal = transientGroundingReport.GroundNormal;
            InnerGroundNormal = transientGroundingReport.InnerGroundNormal;
            OuterGroundNormal = transientGroundingReport.OuterGroundNormal;

            GroundCollider = null;
            GroundPoint = float3.zero;
        }
    }
    
    /// <summary>
    /// Contains the simulation-relevant information for the motor's grounding status
    /// </summary>
    public struct CharacterTransientGroundingReport
    {
        public bool FoundAnyGround;
        public bool IsStableOnGround;
        public bool SnappingPrevented;
        public float3 GroundNormal;
        public float3 InnerGroundNormal;
        public float3 OuterGroundNormal;

        public void CopyFrom(CharacterGroundingReport groundingReport)
        {
            FoundAnyGround = groundingReport.FoundAnyGround;
            IsStableOnGround = groundingReport.IsStableOnGround;
            SnappingPrevented = groundingReport.SnappingPrevented;
            GroundNormal = groundingReport.GroundNormal;
            InnerGroundNormal = groundingReport.InnerGroundNormal;
            OuterGroundNormal = groundingReport.OuterGroundNormal;
        }
    }
    
    /// <summary>
    /// Contains all the information from a hit stability evaluation
    /// </summary>
    public struct HitStabilityReport
    {
        public bool IsStable;

        public bool FoundInnerNormal;
        public float3 InnerNormal;
        public bool FoundOuterNormal;
        public float3 OuterNormal;

        public bool ValidStepDetected;
        public GameplayCollider SteppedCollider;

        public bool LedgeDetected;
        public bool IsOnEmptySideOfLedge;
        public float DistanceFromLedge;
        public bool IsMovingTowardsEmptySideOfLedge;
        public float3 LedgeGroundNormal;
        public float3 LedgeRightDirection;
        public float3 LedgeFacingDirection;
    }

    /// <summary>
    /// Contains the information of hit rigidbodies during the movement phase, so they can be processed afterwards
    /// </summary>
    public struct RigidbodyProjectionHit
    {
        public GameplayRigidbody Rigidbody;
        public float3 HitPoint;
        public float3 EffectiveHitNormal;
        public float3 HitVelocity;
        public bool StableOnHit;
    }
}