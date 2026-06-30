using Bliss.Core;

namespace Bliss.Physics
{
    [EntitySystemOf(typeof(GameplayBoxCollider))]
    public static partial class GameplayBoxColliderSystems_Client
    {
        private static void Awake(this GameplayBoxCollider self, BoxColliderInitParams param1)
        {
             EventSystem.Instance.Invoke(PhysicsInvokeType.BoxColliderAwake, new GameplayBoxColliderAwakeParams()
             {
                 Collider = self,
                 InitParams = param1,
             });
             
             self.Center = param1.Center;
             self.Size = param1.Size;
             self.IsTrigger =  param1.IsTrigger;
             self.AttachedRigidbody = self.Parent.GetComponent<GameplayRigidbody>();
        }
        private static void Destroy(this GameplayBoxCollider self)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.BoxColliderDestroy, new GameplayBoxColliderDestroyParams()
            {
                Collider = self,
            });
        }
    }
}