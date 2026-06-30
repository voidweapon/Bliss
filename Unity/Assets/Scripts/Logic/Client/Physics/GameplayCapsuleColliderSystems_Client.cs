using Bliss.Core;

namespace Bliss.Physics
{
    [EntitySystemOf(typeof(GameplayCapsuleCollider))]
    public static partial class GameplayCapsuleColliderSystems_Client
    {
        private static void Awake(this GameplayCapsuleCollider self, CapsuleColliderInitParams param1)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.CapsuleColliderAwake, new GameplayCapsuleColliderAwakeParams()
            {
                Collider = self,
                InitParams = param1,
            });
            
            self.Center = param1.Center;
            self.Radius = param1.Radius;
            self.Height = param1.Height;
            self.Direction = param1.Direction;
            self.IsTrigger = param1.IsTrigger;
            self.AttachedRigidbody = self.Parent.GetComponent<GameplayRigidbody>();
        }
        private static void Destroy(this GameplayCapsuleCollider self)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.CapsuleColliderDestroy, new GameplayCapsuleColliderDestroyParams()
            {
                Collider = self,
            });
        }
    }
}