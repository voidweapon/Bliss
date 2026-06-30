using Bliss.Core;

namespace Bliss.Physics
{
    [EntitySystemOf(typeof(GameplayRigidbody))]
    public static partial class GameplayRigidbodySystems_Client
    {
        private static void Awake(this GameplayRigidbody self, GameplayRigidbodyInitParams initParams)
        {
            self.IsStatic = initParams.IsStatic;
            self.isKinematic = initParams.IsKinematic;
            EventSystem.Instance.Invoke(PhysicsInvokeType.RigidbodyAwake, new GameplayRigidbodyAwakeParams()
            {
                Rigidbody = self,
                IsStatic = initParams.IsStatic,
                IsKinematic = initParams.IsKinematic,
                UseGravity = initParams.UseGravity
            });
        }
        private static void Destroy(this GameplayRigidbody self)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.RigidbodyDestroy, new GameplayRigidbodyDestroyParams()
            {
                Rigidbody = self
            });
        }
    }
}