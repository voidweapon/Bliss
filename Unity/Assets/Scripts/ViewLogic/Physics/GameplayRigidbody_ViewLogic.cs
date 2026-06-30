using Bliss.Core.EventSystem;
using Bliss.ViewModel;
using UnityEngine;
using Unity.Mathematics;

namespace Bliss.ViewLogic.Physics
{
    [Invoke(PhysicsInvokeType.RigidbodyAwake)]
    public class EventHanlder_GameplayRigidbodyAwake : AInvokeHandler<GameplayRigidbodyAwakeParams>
    {
        public override void Handle(GameplayRigidbodyAwakeParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetOrCreateUnityRigidbody();
            
            if(goRigidbody == null || param.IsStatic)
            {
                return;
            }
            
            goRigidbody.isKinematic = param.IsKinematic;
            goRigidbody.useGravity =  param.UseGravity;
        }
    }

    [Invoke(PhysicsInvokeType.RigidbodyDestroy)]
    public class EventHanlder_GameplayRigidbodyDestroy : AInvokeHandler<GameplayRigidbodyDestroyParams>
    {
        public override void Handle(GameplayRigidbodyDestroyParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if(goRigidbody == null)
            {
                return;
            }
            
            Object.Destroy(goRigidbody);
        }
    }
    
    [Invoke(PhysicsInvokeType.WakeUp)]
    public class EventHandler_GameplayRigidbodyWakeUp: AInvokeHandler<WakeUpParams>
    {
        public override void Handle(WakeUpParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if (goRigidbody == null)
            {
                return;
            }

            goRigidbody.WakeUp();
        }
    }
    
    [Invoke(PhysicsInvokeType.SetMass)]
    public class EventHandler_GameplayRigidbodySetMass : AInvokeHandler<SetMassParams>
    {
        public override void Handle(SetMassParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if(goRigidbody == null)
            {
                return;
            }
            
            goRigidbody.mass = param.Mass;
        }
    }
    

    // ----- New handlers added below -----

    [Invoke(PhysicsInvokeType.AddForceAtPosition)]
    public class EventHandler_GameplayRigidbodyAddForceAtPosition : AInvokeHandler<AddForceAtPositionParams>
    {
        public override void Handle(AddForceAtPositionParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if (goRigidbody == null)
            {
                return;
            }

            // Convert float3 to Vector3 and apply force at position with given mode
            goRigidbody.AddForceAtPosition(param.Force.ToVector3(), param.Position.ToVector3(), param.Mode.ToUnityForceMode());
        }
    }

    [Invoke(PhysicsInvokeType.MovePosition)]
    public class EventHandler_GameplayRigidbodyMovePosition : AInvokeHandler<MovePositionParams>
    {
        public override void Handle(MovePositionParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if (goRigidbody == null)
            {
                return;
            }

            goRigidbody.MovePosition(param.NewPosition.ToVector3());
        }
    }

    [Invoke(PhysicsInvokeType.MoveRotation)]
    public class EventHandler_GameplayRigidbodyMoveRotation : AInvokeHandler<MoveRotationParams>
    {
        public override void Handle(MoveRotationParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if (goRigidbody == null)
            {
                return;
            }

            goRigidbody.MoveRotation(param.NewRotation.ToQuaternion());
        }
    }

    [Invoke(PhysicsInvokeType.MovePositionAndRotation)]
    public class EventHandler_GameplayRigidbodyMovePositionAndRotation : AInvokeHandler<MovePositionAndRotationParams>
    {
        public override void Handle(MovePositionAndRotationParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if (goRigidbody == null)
            {
                return;
            }

            // Apply both position and rotation via Move* APIs
            goRigidbody.Move(param.Position.ToVector3(), param.Rotation.ToQuaternion());
        }
    }

    [Invoke(PhysicsInvokeType.SetPosition)]
    public class EventHandler_GameplayRigidbodySetPosition : AInvokeHandler<SetPositionParams>
    {
        public override void Handle(SetPositionParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if (goRigidbody == null)
            {
                return;
            }

            // Directly set Rigidbody position
            goRigidbody.position = param.Position.ToVector3();
        }
    }

    [Invoke(PhysicsInvokeType.SetRotation)]
    public class EventHandler_GameplayRigidbodySetRotation : AInvokeHandler<SetRotationParams>
    {
        public override void Handle(SetRotationParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if (goRigidbody == null)
            {
                return;
            }

            // Directly set Rigidbody rotation
            goRigidbody.rotation = param.Rotation.ToQuaternion();
        }
    }
    

    [Invoke(PhysicsInvokeType.SetCenterOfMass)]
    public class EventHandler_GameplayRigidbodySetCenterOfMass : AInvokeHandler<SetCenterOfMassParams>
    {
        public override void Handle(SetCenterOfMassParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if (goRigidbody == null)
            {
                return;
            }

            goRigidbody.centerOfMass = param.CenterOfMass.ToVector3();
        }
    }

    [Invoke(PhysicsInvokeType.SetVelocity)]
    public class EventHandler_GameplayRigidbodySetVelocity : AInvokeHandler<SetVelocityParams>
    {
        public override void Handle(SetVelocityParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if (goRigidbody == null)
            {
                return;
            }

            goRigidbody.linearVelocity = param.Velocity.ToVector3();
        }
    }

    [Invoke(PhysicsInvokeType.SetAngularVelocity)]
    public class EventHandler_GameplayRigidbodySetAngularVelocity : AInvokeHandler<SetAngularVelocityParams>
    {
        public override void Handle(SetAngularVelocityParams param)
        {
            var rigidbody = param.Rigidbody;
            var goRigidbody = rigidbody.GetUnityRigidbody();
            if (goRigidbody == null)
            {
                return;
            }

            goRigidbody.angularVelocity = param.AngularVelocity.ToVector3();
        }
    }

    
    public static class GameplayRigidbodyViewHelper
    {
        public static Rigidbody GetUnityRigidbody(this GameplayRigidbody gameplayRigidbody)
        {
            var viewCmp = gameplayRigidbody.Parent.GetComponent<GameObjectViewComponent>();
            if (viewCmp == null || viewCmp.View == null)
            {
                return null;
            }

            var go = viewCmp.View;
            var goRigidbody = go.GetComponent<Rigidbody>();
            if (goRigidbody == null)
            {
                return null;
            }

            return goRigidbody;
        }

        public static T GetUnityCollider<T>(this GameplayCollider gameplayCollider) where T : Collider
        {
            var viewCmp = gameplayCollider.Parent.GetComponent<GameObjectViewComponent>();
            if (viewCmp == null || viewCmp.View == null)
            {
                return null;
            }

            var go = viewCmp.View;
            var collider = go.GetComponent<T>();
            if (collider == null)
            {
                return null;
            }

            return collider;
        }

        public static Rigidbody GetOrCreateUnityRigidbody(this GameplayRigidbody gameplayRigidbody)
        {
            var viewCmp = gameplayRigidbody.Parent.GetComponent<GameObjectViewComponent>();
            if (viewCmp == null || viewCmp.View == null)
            {
                return null;
            }

            var go = viewCmp.View;
            var goRigidbody = go.GetComponent<Rigidbody>();
            if (goRigidbody == null)
            {
                goRigidbody =  go.AddComponent<Rigidbody>();
            }

            return goRigidbody;
        }
        
        public static T GetOrCreateUnityCollider<T>(this GameplayCollider gameplayCollider) where T : Collider
        {
            var viewCmp = gameplayCollider.Parent.GetComponent<GameObjectViewComponent>();
            if (viewCmp == null || viewCmp.View == null)
            {
                return null;
            }

            var go = viewCmp.View;
            var collider = go.GetComponent<T>();
            if (collider == null)
            {
                collider = go.AddComponent<T>();
            }

            return collider;
        }
        
        // Convert float3 -> Vector3
        public static Vector3 ToVector3(this float3 f)
        {
            return new Vector3(f.x, f.y, f.z);
        }

        // Convert Unity.Mathematics.quaternion -> UnityEngine.Quaternion
        public static Quaternion ToQuaternion(this quaternion q)
        {
            // quaternion.value is a float4 (x,y,z,w)
            var v = q.value;
            return new Quaternion(v.x, v.y, v.z, v.w);
        }

        // Convert Bliss.ForceMode -> UnityEngine.ForceMode
        public static UnityEngine.ForceMode ToUnityForceMode(this Bliss.ForceMode mode)
        {
            switch (mode)
            {
                case Bliss.ForceMode.Force:
                    return UnityEngine.ForceMode.Force;
                case Bliss.ForceMode.Impulse:
                    return UnityEngine.ForceMode.Impulse;
                case Bliss.ForceMode.VelocityChange:
                    return UnityEngine.ForceMode.VelocityChange;
                case Bliss.ForceMode.Acceleration:
                    return UnityEngine.ForceMode.Acceleration;
                default:
                    return UnityEngine.ForceMode.Force;
            }
        }
    }
}