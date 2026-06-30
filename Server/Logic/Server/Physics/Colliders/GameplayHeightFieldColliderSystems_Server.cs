using Bliss.Core;
using Bliss.Server.Physics;

namespace Bliss.Physics;

[EntitySystemOf(typeof(GameplayHeightFieldCollider))]
public static unsafe partial class GameplayHeightFieldColliderSystems_Server
{
    private static void Awake(this GameplayHeightFieldCollider self, GameplayHeightFieldColliderInitParams param1)
    {
        self.ShapePtr = param1.PxShape;
        self.IsTrigger = false;

        self.AttachedRigidbody = self.Parent.GetComponent<GameplayRigidbody>();
    }
    private static void Destroy(this GameplayHeightFieldCollider self)
    {
        self.Scene()?.GetComponent<PhysicalScene>()?.RemoveGameplayCollider(self);
        self.AttachedRigidbody?.RemoveCollider(self);
        self.ShapePtr = null;
        self.AttachedRigidbody = null;
    }
}