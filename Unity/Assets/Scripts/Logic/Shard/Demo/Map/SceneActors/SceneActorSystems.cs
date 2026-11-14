using System;
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
            bool networkEntityRegistered = false;
            bool sceneActorRegistered = false;

            void Register()
            {
                if (networkEntityManager != null)
                {
                    networkEntityManager.AddEntity(self.Id, self);
                    networkEntityRegistered = true;
                }

                if (sceneActorManager != null)
                {
                    sceneActorManager.Add(self);
                    sceneActorRegistered = true;
                }
            }

            void Rollback()
            {
                if (sceneActorRegistered)
                {
                    sceneActorManager.Remove(self.Id);
                    sceneActorRegistered = false;
                }

                if (networkEntityRegistered)
                {
                    networkEntityManager.RemoveEntity(self.Id);
                    networkEntityRegistered = false;
                }
            }

            LevelGameWorldComponent levelAssociation = self.GetSceneComponent<LevelGameWorldComponent>();
            if (levelAssociation == null)
            {
                Register();
                return;
            }

            levelAssociation.DeferUntilCommit(Register, Rollback);
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