using Bliss.Core;

namespace Bliss.Physics
{
    [EntitySystemOf(typeof(GameplayHeightFieldCollider))]
    public static partial class GameplayHeightFieldColliderSystems_Client
    {
        private static void Awake(this GameplayHeightFieldCollider self, GameplayHeightFieldColliderInitParams  initParams)
        {
            self.IsTrigger = false;
            EventSystem.Instance.Invoke(PhysicsInvokeType.HeightFieldColliderAwake, new GameplayHeightFieldColliderAwakeParams()
            {
                Collider = self,
            });
        }
        private static void Destroy(this GameplayHeightFieldCollider self)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.HeightFieldColliderDestroy, new GameplayHeightFieldDestroyParams()
            {
                Collider = self,
            });
        }
    }
}