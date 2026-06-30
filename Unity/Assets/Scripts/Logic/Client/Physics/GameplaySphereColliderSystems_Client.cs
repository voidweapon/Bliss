using Bliss.Core;

namespace Bliss.Physics
{
    [EntitySystemOf(typeof(GameplaySphereCollider))]
    public static partial class GameplaySphereColliderSystems_Client
    {
        private static void Awake(this GameplaySphereCollider self, SphereColliderInitParams param1)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.SphereColliderAwake, new GameplaySphereColliderAwakeParams()
            {
                Collider = self,
                InitParams = param1,
            });
             
            self.Center = param1.Center;
            self.Radius = param1.Radius;
            self.IsTrigger = param1.IsTrigger;
            self.AttachedRigidbody = self.Parent.GetComponent<GameplayRigidbody>();
        }
        private static void Destroy(this GameplaySphereCollider self)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.SphereColliderDestroy, new GameplaySphereColliderDestroyParams()
            {
                Collider = self,
            });
        }
    }
}