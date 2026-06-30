using Bliss.Core;

namespace Bliss.Physics
{
    [EntitySystemOf(typeof(GameplayMeshCollider))]
    public static partial class GameplayMeshColliderSystems_Client
    {
        private static void Awake(this GameplayMeshCollider self, GameplayMeshColliderInitParams  initParams)
        {
            self.Convex = initParams.Convex;
            self.IsTrigger = initParams.IsTrigger;
            EventSystem.Instance.Invoke(PhysicsInvokeType.MeshColliderAwake, new GameplayMeshColliderAwakeParams()
            {
                Collider = self,
            });
        }
        private static void Destroy(this GameplayMeshCollider self)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.MeshColliderDestroy, new GameplayMeshColliderDestroyParams()
            {
                Collider = self,
            });
        }
    }
}