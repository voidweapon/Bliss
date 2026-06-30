using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Physics;
using MagicPhysX;
using static MagicPhysX.NativeMethods; 

namespace Bliss.Server.Physics;

[EntitySystemOf(typeof(GameplayRigidbody))]
public static unsafe partial class GameplayRigidbodySystems_Server
{
    private static void Awake(this GameplayRigidbody self, GameplayRigidbodyInitParams param1)
    {
        self.RigidBodyPtr = param1.RigidBodyPtr;
        self.isKinematic = param1.IsKinematic;
        self.IsStatic = param1.IsStatic;
    }
    private static void Destroy(this GameplayRigidbody self)
    {
        self.Scene()?.GetComponent<PhysicalScene>()?.RemoveGameplayRigidbody(self);
        
        if (self.RigidBodyPtr != null)
        {
            PxRigidActor_release_mut((PxRigidActor*)self.RigidBodyPtr);
        }

        self.RigidBodyPtr = null;
    }

    public static void RemoveCollider(this GameplayRigidbody self, GameplayCollider collider)
    {
        PxRigidActor_detachShape_mut((PxRigidActor*)self.RigidBodyPtr, collider.ShapePtr, false);
    }
    
    [Invoke(PhysicsInvokeType.RigidbodyAwake)]
    public class EventHanlder_GameplayRigidbodyAwake : AInvokeHandler<GameplayRigidbodyAwakeParams>
    {
        public override void Handle(GameplayRigidbodyAwakeParams param)
        {
            var rigidbody = param.Rigidbody;
            PxRigidDynamic* pxRigidDynamic = (PxRigidDynamic*)rigidbody.RigidBodyPtr;
            
            if(pxRigidDynamic == null || param.IsStatic)
            {
                return;
            }

            pxRigidDynamic->WakeUpMut();
        }
    }

    [Invoke(PhysicsInvokeType.RigidbodyDestroy)]
    public class EventHanlder_GameplayRigidbodyDestroy : AInvokeHandler<GameplayRigidbodyDestroyParams>
    {
        public override void Handle(GameplayRigidbodyDestroyParams param)
        {
            var rigidbody = param.Rigidbody;
            PxRigidActor* pxRigidActor = (PxRigidActor*)rigidbody.RigidBodyPtr;
            
            if(pxRigidActor == null)
            {
                return;
            }

            pxRigidActor->ReleaseMut();
        }
    }
    
    [Invoke(PhysicsInvokeType.SetMass)]
    public class EventHandler_GameplayRigidbodySetMass : AInvokeHandler<SetMassParams>
    {
        public override void Handle(SetMassParams param)
        {
            var rigidbody = param.Rigidbody;
            if(rigidbody.RigidBodyPtr == null)
            {
                return;
            }
            
            rigidbody.RigidBodyPtr->SetMassMut(param.Mass);
        }
    }
    

    [Invoke(PhysicsInvokeType.AddForceAtPosition)]
    public class EventHandler_GameplayRigidbodyAddForceAtPosition : AInvokeHandler<AddForceAtPositionParams>
    {
        public override void Handle(AddForceAtPositionParams param)
        {
            var rigidbody = param.Rigidbody;
            if(rigidbody.RigidBodyPtr == null)
            {
                return;
            }

            PxVec3 force = param.Force.ToPxVec3();
            PxVec3 position =  param.Position.ToPxVec3();
            rigidbody.RigidBodyPtr->ExtAddForceAtPos(&force, &position, param.Mode.ToPxForceMode(), true);;
        }
    }

    [Invoke(PhysicsInvokeType.MovePosition)]
    public class EventHandler_GameplayRigidbodyMovePosition : AInvokeHandler<MovePositionParams>
    {
        public override void Handle(MovePositionParams param)
        {
            var rigidbody = param.Rigidbody;
            if(rigidbody.RigidBodyPtr == null || rigidbody.IsStatic)
            {
                return;
            }

            var dynamicRigidBody = (PxRigidDynamic*)rigidbody.RigidBodyPtr;
            if (dynamicRigidBody == null)
            {
                return;
            }
            
            PxVec3 pxOrigin = param.NewPosition.ToPxVec3();
            PxQuat pxRRotation = rigidbody.Rotation.ToPxQuat();
            var pxTransform = PxTransform_new_5(&pxOrigin, &pxRRotation);
            dynamicRigidBody->SetKinematicTargetMut(&pxTransform);
        }
    }

    [Invoke(PhysicsInvokeType.MoveRotation)]
    public class EventHandler_GameplayRigidbodyMoveRotation : AInvokeHandler<MoveRotationParams>
    {
        public override void Handle(MoveRotationParams param)
        {
            var rigidbody = param.Rigidbody;
            if(rigidbody.RigidBodyPtr == null || rigidbody.IsStatic)
            {
                return;
            }

            var dynamicRigidBody = (PxRigidDynamic*)rigidbody.RigidBodyPtr;
            if (dynamicRigidBody == null)
            {
                return;
            }
            
            PxVec3 pxOrigin = rigidbody.Position.ToPxVec3();
            PxQuat pxRRotation = param.NewRotation.ToPxQuat();
            var pxTransform = PxTransform_new_5(&pxOrigin, &pxRRotation);
            dynamicRigidBody->SetKinematicTargetMut(&pxTransform);
        }
    }

    [Invoke(PhysicsInvokeType.MovePositionAndRotation)]
    public class EventHandler_GameplayRigidbodyMovePositionAndRotation : AInvokeHandler<MovePositionAndRotationParams>
    {
        public override void Handle(MovePositionAndRotationParams param)
        {
            var rigidbody = param.Rigidbody;
            if(rigidbody.RigidBodyPtr == null || rigidbody.IsStatic)
            {
                return;
            }

            var dynamicRigidBody = (PxRigidDynamic*)rigidbody.RigidBodyPtr;
            if (dynamicRigidBody == null)
            {
                return;
            }
            
            PxVec3 pxOrigin = param.Position.ToPxVec3();
            PxQuat pxRRotation = param.Rotation.ToPxQuat();
            var pxTransform = PxTransform_new_5(&pxOrigin, &pxRRotation);
            dynamicRigidBody->SetKinematicTargetMut(&pxTransform);
        }
    }

    [Invoke(PhysicsInvokeType.SetPosition)]
    public class EventHandler_GameplayRigidbodySetPosition : AInvokeHandler<SetPositionParams>
    {
        public override void Handle(SetPositionParams param)
        {
            var rigidbody = param.Rigidbody;
            if(rigidbody.RigidBodyPtr == null)
            {
                return;
            }
            PxVec3 pxOrigin = param.Position.ToPxVec3();
            PxQuat pxRRotation = rigidbody.Rotation.ToPxQuat();
            var pxTransform = PxTransform_new_5(&pxOrigin, &pxRRotation);
            PxRigidActor_setGlobalPose_mut((PxRigidActor*)rigidbody.RigidBodyPtr, &pxTransform, true);
        }
    }

    [Invoke(PhysicsInvokeType.SetRotation)]
    public class EventHandler_GameplayRigidbodySetRotation : AInvokeHandler<SetRotationParams>
    {
        public override void Handle(SetRotationParams param)
        {
            var rigidbody = param.Rigidbody;
            if(rigidbody.RigidBodyPtr == null)
            {
                return;
            }
            PxVec3 pxOrigin = rigidbody.Position.ToPxVec3();
            PxQuat pxRRotation = param.Rotation.ToPxQuat();
            var pxTransform = PxTransform_new_5(&pxOrigin, &pxRRotation);
            PxRigidActor_setGlobalPose_mut((PxRigidActor*)rigidbody.RigidBodyPtr, &pxTransform, true);
        }
    }

    [Invoke(PhysicsInvokeType.SetCenterOfMass)]
    public class EventHandler_GameplayRigidbodySetCenterOfMass : AInvokeHandler<SetCenterOfMassParams>
    {
        public override void Handle(SetCenterOfMassParams param)
        {
            var rigidbody = param.Rigidbody;
            if(rigidbody.RigidBodyPtr == null)
            {
                return;
            }

            var center = param.CenterOfMass.ToPxVec3();
            var pose =  PxTransform_new_1(&center);
            PxRigidBody_setCMassLocalPose_mut((PxRigidBody*)rigidbody.RigidBodyPtr, &pose);
        }
    }

    [Invoke(PhysicsInvokeType.SetVelocity)]
    public class EventHandler_GameplayRigidbodySetVelocity : AInvokeHandler<SetVelocityParams>
    {
        public override void Handle(SetVelocityParams param)
        {
            var rigidbody = param.Rigidbody;
            if(rigidbody.RigidBodyPtr == null || rigidbody.IsStatic)
            {
                return;
            }

            var dynamicRigidBody = (PxRigidDynamic*)rigidbody.RigidBodyPtr;
            if (dynamicRigidBody == null)
            {
                return;
            }
            
            var velocity = param.Velocity.ToPxVec3();
            dynamicRigidBody->SetLinearVelocityMut(&velocity, true);
        }
    }

    [Invoke(PhysicsInvokeType.SetAngularVelocity)]
    public class EventHandler_GameplayRigidbodySetAngularVelocity : AInvokeHandler<SetAngularVelocityParams>
    {
        public override void Handle(SetAngularVelocityParams param)
        {
            var rigidbody = param.Rigidbody;
            if(rigidbody.RigidBodyPtr == null || rigidbody.IsStatic)
            {
                return;
            }

            var dynamicRigidBody = (PxRigidDynamic*)rigidbody.RigidBodyPtr;
            if (dynamicRigidBody == null)
            {
                return;
            }

            var angularVelocity = param.AngularVelocity.ToPxVec3();
            dynamicRigidBody->SetAngularVelocityMut(&angularVelocity, true);
        }
    }
}


