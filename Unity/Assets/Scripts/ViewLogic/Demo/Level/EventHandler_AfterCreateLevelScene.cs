using System;
using Bliss.Base.Collections;
using Bliss.CharacterControllers;
using Bliss.ViewLogic;
using Bliss.ViewModel;
using ET;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bliss
{
    /// <summary>
    /// Loads the destination scene additively and activates it only after LevelScene initialization commits.
    /// <seealso cref="MapHelper.ChangeMap"/>
    /// </summary>
    [Event(ELogicSceneType.Level)]
    public class EventHandler_AfterCreateLevelScene : AEvent<LogicScene, Event_AfterCreateLevelScene>
    {
        protected override async ETTask  Run(LogicScene levelScene, Event_AfterCreateLevelScene eventInfo)
        {
            levelScene.AddComponent<UIComponent>();
            var resourcesLoaderCmp = levelScene.AddComponent<ResourcesLoaderComponent>();
            
            LevelConfig levelConfig = ConfigManager.Instance.Tables.TbLevelConfig.GetOrDefault(eventInfo.LevelConfigId);
            
            var levelHandler = await resourcesLoaderCmp.LoadSceneAsync($"Assets/Res/Scenes/{levelConfig.Name}.unity", LoadSceneMode.Additive);

            var clientScene = levelScene.GetComponent<LevelGameWorldComponent>().GameplayWorld.Parent;
            var clientResourcesLoader = clientScene.GetComponent<ResourcesLoaderComponent>();
            if (clientResourcesLoader == null)
            {
                throw new InvalidOperationException("The owning client scene has no resource loader.");
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            LevelGameWorldComponent levelAssociation = levelScene.GetComponent<LevelGameWorldComponent>();
            levelAssociation.DeferUntilCommit(
                () =>
                {
                    if (!levelHandler.ActivateScene())
                    {
                        throw new InvalidOperationException($"Failed to activate scene {levelHandler.SceneName}.");
                    }
                },
                () =>
                {
                    if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    {
                        if (!SceneManager.SetActiveScene(previousActiveScene))
                        {
                            throw new InvalidOperationException(
                                $"Failed to restore active scene {previousActiveScene.name} after LevelScene commit failure.");
                        }
                    }
                });
            levelAssociation.AfterCommit(
                () => completeSceneTransition(clientScene, clientResourcesLoader, previousActiveScene).Coroutine());
            await ETTask.CompletedTask;
        }

        private static async ETTask completeSceneTransition(
            LogicScene clientScene,
            ResourcesLoaderComponent resourcesLoader,
            Scene previousActiveScene)
        {
            try
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    await resourcesLoader.UnloadSceneAsync(previousActiveScene);
                }

                clientScene.GetComponent<ObjectWait>().Notify(new Wait_LoadLevelSceneView(), true);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                clientScene.GetComponent<ObjectWait>().Notify(
                    new Wait_LoadLevelSceneView { Error = WaitTypeError.Cancel });
                clientScene.GetComponent<ObjectWait>().Notify(
                    new Wait_FinishChangeLevelScene { Error = WaitTypeError.Cancel });
            }
        }
    }
}