using Bliss.Core;
using Bliss.Server.Physics;

namespace Bliss.Physics;

[EntitySystemOf(typeof(GameplayMeshCollider))]
public static unsafe partial class GameplayMeshColliderSystems_Server
{
    private static void Awake(this GameplayMeshCollider self, GameplayMeshColliderInitParams param1)
    {
        self.ShapePtr = param1.PxShape;
        
        self.Convex = param1.Convex;
        self.IsTrigger = param1.IsTrigger;
        
        self.AttachedRigidbody = self.Parent.GetComponent<GameplayRigidbody>();
    }
    private static void Destroy(this GameplayMeshCollider self)
    {
        self.Scene()?.GetComponent<PhysicalScene>()?.RemoveGameplayCollider(self);
        self.AttachedRigidbody?.RemoveCollider(self);
        self.ShapePtr = null;
        self.AttachedRigidbody = null;
    }
}