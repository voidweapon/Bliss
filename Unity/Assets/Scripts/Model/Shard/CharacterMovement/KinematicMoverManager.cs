using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;

namespace Bliss.CharacterMovement
{
    [SystemPriority(typeof(ILateUpdate), 1)]
    public class KinematicMoverManager : Entity, IAwake<KinematicMoverManagerSettings>, IDestroy, ILateUpdate
    {
        public KinematicMoverManagerSettings Settings;
        
        public IndexList<long, EntityRef<KinematicCharacterComponent>> CharacterMotors = new IndexList<long, EntityRef<KinematicCharacterComponent>>();
        public IndexList<long, EntityRef<KinematicRigidbodyMover>> PhysicsMovers = new IndexList<long, EntityRef<KinematicRigidbodyMover>>();
        public long LogicUpdateTimer;
        
        public long LastCustomInterpolationStartTime = -1L;
        public float LastCustomInterpolationDeltaTime = -1f;

        public long PreviousTimeStamp;
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