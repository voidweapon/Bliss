using Bliss.Base.Collections;
using Bliss.Core;
using System.Collections.Generic;

namespace Bliss.CharacterMovement
{
    /// <summary>Owns Kinematic simulation and interpolation for its GameplayWorld.</summary>
    [SystemPriority(typeof(ILateUpdate), 1)]
    public class KinematicMoverManager : GamePlayEntity, IAwake<KinematicMoverManagerSettings>, IDestroy, ILateUpdate
    {
        public KinematicMoverManagerSettings Settings;
        
        public IndexList<long, EntityRef<KinematicCharacterComponent>> CharacterMotors = new IndexList<long, EntityRef<KinematicCharacterComponent>>();
        public IndexList<long, EntityRef<KinematicRigidbodyMover>> PhysicsMovers = new IndexList<long, EntityRef<KinematicRigidbodyMover>>();

        /// <summary>Reusable motor references captured at the start of the current simulation.</summary>
        public readonly List<EntityRef<KinematicCharacterComponent>> MotorSnapshot = new List<EntityRef<KinematicCharacterComponent>>();
        /// <summary>Reusable mover references captured at the start of the current simulation.</summary>
        public readonly List<EntityRef<KinematicRigidbodyMover>> MoverSnapshot = new List<EntityRef<KinematicRigidbodyMover>>();
        /// <summary>Whether the manager is currently using its reusable participant snapshots.</summary>
        public bool IsDispatching;
        
        // public Queue<EntityRef<KinematicCharacterComponent>> CharacterMotors = new Queue<EntityRef<KinematicCharacterComponent>>();
        // public Queue<EntityRef<KinematicCharacterComponent>> PhysicsMovers = new Queue<EntityRef<KinematicCharacterComponent>>();
        public long LastCustomInterpolationStartTime = -1L;
        public float LastCustomInterpolationDeltaTime = -1f;

        public bool Enable = false;
    }
    
    public struct KinematicMoverManagerSettings
    {
        public bool AutoSimulation;
        public bool Interpolate;
        public bool InterpolateMotor;
        public bool InterpolateMover;
    }
}