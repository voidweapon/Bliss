using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(SceneActor))]
    public static partial class SceneActorSystems
    {
        private static void Awake(this SceneActor self)
        {
            NetworkEntityManagerComponent networkEntityManager =
                self.RootScene().GetComponent<NetworkEntityManagerComponent>();
            SceneActorManager sceneActorManager =
                self.GetLevelGameplayWorld()?.GetComponent<SceneActorManager>();

            if (networkEntityManager != null)
            {
                networkEntityManager.AddEntity(self.Id, self);
            }

            sceneActorManager?.Add(self);
        }
        private static void Destroy(this SceneActor self)
        {
            var networkEntityManager = self.RootScene().GetComponent<NetworkEntityManagerComponent>();
            networkEntityManager?.RemoveEntity(self.Id);

            var sceneActorManager = self.GetLevelGameplayWorld()?.GetComponent<SceneActorManager>();
            sceneActorManager?.Remove(self.Id);
        }
    }
}