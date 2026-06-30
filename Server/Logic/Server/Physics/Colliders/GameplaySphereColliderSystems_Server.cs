using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Server.Physics;
using MagicPhysX;
using static MagicPhysX.NativeMethods; 

namespace Bliss.Physics;

[EntitySystemOf(typeof(GameplaySphereCollider))]
public static unsafe partial class GameplaySphereColliderSystems_Server
{
    private static void Awake(this GameplaySphereCollider self, SphereColliderInitParams param1)
    {
        self.ShapePtr = param1.PxShape;
        //TODO: 初始化时重复赋值了
        self.Center =  param1.Center;
        self.Radius =  param1.Radius;
        self.IsTrigger = param1.IsTrigger;

        self.AttachedRigidbody = self.Parent.GetComponent<GameplayRigidbody>();
    }
    private static void Destroy(this GameplaySphereCollider self)
    {
        self.Scene()?.GetComponent<PhysicalScene>()?.RemoveGameplayCollider(self);
        self.AttachedRigidbody?.RemoveCollider(self);
        self.ShapePtr = null;
        self.AttachedRigidbody = null;
    }
    
    [Invoke(PhysicsInvokeType.SphereColliderSetCenter)]
    public class EventHandler_GameplaySphereColliderSetCenter: AInvokeHandler<SetSphereColliderCenterParams>
    {
        public override void Handle(SetSphereColliderCenterParams param)
        {
            var sphereCollider = param.Collider;
            if(sphereCollider.ShapePtr == null) return;
            
            var pxTransform = sphereCollider.ShapePtr->GetLocalPose();
            pxTransform.p = param.Center.ToPxVec3();
            sphereCollider.ShapePtr->SetLocalPoseMut(&pxTransform);
        }
    }
    
    [Invoke(PhysicsInvokeType.SphereColliderSetRadius)]
    public class EventHandler_GameplaySphereolliderSetRadius: AInvokeHandler<SetSphereColliderRadiusParams>
    {
        public override void Handle(SetSphereColliderRadiusParams param)
        {
            var sphereCollider = param.Collider;
            if(sphereCollider.SphereGeometry == null) return;

            sphereCollider.SphereGeometry->radius = param.Radius;
        }
    }
}