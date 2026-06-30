using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Server.Physics;
using MagicPhysX;
using static MagicPhysX.NativeMethods; 


namespace Bliss.Physics;

[EntitySystemOf(typeof(GameplayBoxCollider))]
public static unsafe partial class GameplayBoxColliderSystems_Server
{
    private static void Awake(this GameplayBoxCollider self, BoxColliderInitParams param1)
    {
        self.ShapePtr = param1.PxShape;
        //TODO: 初始化时重复赋值了
        self.Center =  param1.Center;
        self.Size =  param1.Size;
        self.IsTrigger = param1.IsTrigger;

        self.AttachedRigidbody = self.Parent.GetComponent<GameplayRigidbody>();
    }
    private static void Destroy(this GameplayBoxCollider self)
    {
        self.Scene()?.GetComponent<PhysicalScene>()?.RemoveGameplayCollider(self);
        self.AttachedRigidbody?.RemoveCollider(self);
        self.ShapePtr = null;
        self.AttachedRigidbody = null;
    }
    
    [Invoke(PhysicsInvokeType.BoxColliderSetCenter)]
    public class EventHandler_GameplayBoxColliderSetCenter: AInvokeHandler<SetBoxColliderCenterParams>
    {
        public override void Handle(SetBoxColliderCenterParams param)
        {
            var boxCollider = param.Collider;
            if(boxCollider.ShapePtr == null) return;
            
            var pxTransform = boxCollider.ShapePtr->GetLocalPose();
            pxTransform.p = param.Center.ToPxVec3();
            boxCollider.ShapePtr->SetLocalPoseMut(&pxTransform);
        }
    }
    
    [Invoke(PhysicsInvokeType.BoxColliderSetSize)]
    public class EventHandler_GameplayBoxColliderSetSize: AInvokeHandler<SetBoxColliderSizeParams>
    {
        public override void Handle(SetBoxColliderSizeParams param)
        {
            var boxCollider = param.Collider;
            if(boxCollider.BoxGeometry == null) return;
            
            boxCollider.BoxGeometry->halfExtents = (param.Size * 0.5f).ToPxVec3();
        }
    }
}