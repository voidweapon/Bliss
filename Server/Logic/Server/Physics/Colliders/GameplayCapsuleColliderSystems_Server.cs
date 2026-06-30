using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Server.Physics;
using MagicPhysX;
using static MagicPhysX.NativeMethods; 

namespace Bliss.Physics;

[EntitySystemOf(typeof(GameplayCapsuleCollider))]
public static unsafe partial class GameplayCapsuleColliderSystems_Server
{
    private static void Awake(this GameplayCapsuleCollider self, CapsuleColliderInitParams initParams)
    {
        self.ShapePtr = initParams.PxShape;

        self.Center = initParams.Center;
        self.Radius = initParams.Radius;
        self.Height = initParams.Height;
        self.Direction = initParams.Direction;
        self.IsTrigger = initParams.IsTrigger;
        
        self.AttachedRigidbody = self.Parent.GetComponent<GameplayRigidbody>();
    }
    
    private static void Destroy(this GameplayCapsuleCollider self)
    {
        self.Scene()?.GetComponent<PhysicalScene>()?.RemoveGameplayCollider(self);
        self.AttachedRigidbody?.RemoveCollider(self);
        self.ShapePtr = null;
        self.AttachedRigidbody = null;
    }
    
    [Invoke(PhysicsInvokeType.CapsuleColliderSetCenter)]
    public class EventHandler_GameplayCapsuleColliderSetCenter: AInvokeHandler<SetCapsuleColliderCenterParams>
    {
        public override void Handle(SetCapsuleColliderCenterParams param)
        {
            var capsuleCollider = param.Collider;
            if(capsuleCollider.ShapePtr == null) return;
            
            var pxTransform = capsuleCollider.ShapePtr->GetLocalPose();
            pxTransform.p = param.Center.ToPxVec3();
            capsuleCollider.ShapePtr->SetLocalPoseMut(&pxTransform);
        }
    }
    
    [Invoke(PhysicsInvokeType.CapsuleColliderSetRadius)]
    public class EventHandler_GameplayCapsuleColliderSetRadius: AInvokeHandler<SetCapsuleColliderRadiusParams>
    {
        public override void Handle(SetCapsuleColliderRadiusParams param)
        {
            var capsuleCollider = param.Collider;
            if(capsuleCollider.CapsuleGeometry == null) return;
            
            capsuleCollider.CapsuleGeometry->radius =  param.Radius;
        }
    }
    
    [Invoke(PhysicsInvokeType.CapsuleColliderSetHeight)]
    public class EventHandler_GameplayCapsuleColliderSetHeight: AInvokeHandler<SetCapsuleColliderHeightParams>
    {
        public override void Handle(SetCapsuleColliderHeightParams param)
        {
            var capsuleCollider = param.Collider;
            if(capsuleCollider.CapsuleGeometry == null) return;
            
            var radius = capsuleCollider.CapsuleGeometry->radius;
            capsuleCollider.CapsuleGeometry->halfHeight = PxTypeHelper.GetPxCapsuleHalfHeight(param.Height, radius);
        }
    }
    
    [Invoke(PhysicsInvokeType.CapsuleColliderSetDirection)]
    public class EventHandler_GameplayCapsuleColliderSetDirectiont: AInvokeHandler<SetCapsuleColliderDirectionParams>
    {
        public override void Handle(SetCapsuleColliderDirectionParams param)
        {
            var capsuleCollider = param.Collider;
            if(capsuleCollider.ShapePtr == null) return;
            
            var pxTransform = capsuleCollider.ShapePtr->GetLocalPose();
            pxTransform.q = PxTypeHelper.GetPxCapsuleLocalQuat(param.Direction);
            
            capsuleCollider.ShapePtr->SetLocalPoseMut(&pxTransform);

        }
    }
}