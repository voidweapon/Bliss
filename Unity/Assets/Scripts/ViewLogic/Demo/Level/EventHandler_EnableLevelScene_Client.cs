using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Bliss.CharacterMovement;
using Bliss.ViewModel;
using ET;
using UnityEngine.SceneManagement;

namespace Bliss
{
    /// <summary>
    /// Creates client scene entities and activates the destination Unity scene.
    /// </summary>
    [Event(ELogicSceneType.Level)]
    public class EventHandler_EnableLevelScene_Client : AEvent<LogicScene, Event_EnableLevelScene>
    {
        protected override async ETTask Run(LogicScene levelScene, Event_EnableLevelScene eventInfo)
        {
            LevelGameWorldComponent levelAssociation = levelScene.GetComponent<LevelGameWorldComponent>();
            LevelSceneViewTransitionComponent transition =
                levelScene.GetComponent<LevelSceneViewTransitionComponent>();
            if (transition == null)
            {
                throw new System.InvalidOperationException("The LevelScene has no transition state.");
            }

            GameplayWorld gameplayWorld = levelAssociation.GameplayWorld;
            KinematicMoverManager moverManager = gameplayWorld.GetComponent<KinematicMoverManager>();
            if (moverManager != null)
            {
                transition.previousMoverEnabled = moverManager.Enable;
                transition.previousMoverStateCaptured = true;
            }

            CameraComponent cameraComponent = levelScene.RootScene().GetComponent<CameraComponent>();
            if (cameraComponent != null)
            {
                transition.previousCameraTarget = cameraComponent.TargetRef;
                transition.previousCameraTargetCaptured = true;
            }

            if (eventInfo.LocalUnit != null)
            {
                UnitManager unitManager = gameplayWorld.GetComponent<UnitManager>();
                if (unitManager == null)
                {
                    throw new System.InvalidOperationException("The GameplayWorld has no UnitManager.");
                }

                transition.previousObserveUnitId = unitManager.ObserveUnitId;
                transition.previousObserveUnitIdCaptured = true;
                transition.createdUnit = eventInfo.LocalUnit as Unit
                    ?? throw new System.InvalidOperationException("The client enable event's local entity is not a Unit.");
                UnitHelper.CreateUnit(levelScene, transition.createdUnit, true, true);
                unitManager.ObserveUnitId = transition.createdUnit.Id;
            }

            await EventSystem.Instance.PushRequiredAsync(levelScene, new Event_EndCreateLevelScene()
            {
                LevelConfigId = levelAssociation.LevelConfigId,
            });

            if (!transition.destinationScene.IsValid() ||
                !transition.destinationScene.isLoaded ||
                !SceneManager.SetActiveScene(transition.destinationScene))
            {
                throw new System.InvalidOperationException(
                    $"Failed to activate the Unity scene for LevelScene {levelScene.Name}.");
            }
        }
    }

    /// <summary>
    /// Restores client-shared runtime state after LevelScene enabling fails.
    /// </summary>
    [Event(ELogicSceneType.Level)]
    public class EventHandler_StopEnableLevelScene_Client : AEvent<LogicScene, Event_StopEnableLevelScene>
    {
        protected override async ETTask Run(LogicScene levelScene, Event_StopEnableLevelScene eventInfo)
        {
            List<Exception> cleanupExceptions = new List<Exception>();
            LevelSceneViewTransitionComponent transition =
                levelScene.GetComponent<LevelSceneViewTransitionComponent>();
            if (transition != null)
            {
                try
                {
                    Scene activeScene = SceneManager.GetActiveScene();
                    if (activeScene == transition.destinationScene &&
                        (!transition.previousActiveScene.IsValid() ||
                         !transition.previousActiveScene.isLoaded ||
                         !SceneManager.SetActiveScene(transition.previousActiveScene)))
                    {
                        throw new System.InvalidOperationException(
                            $"Failed to restore the active Unity scene after enabling {levelScene.Name} failed.");
                    }
                }
                catch (Exception exception)
                {
                    cleanupExceptions.Add(exception);
                }

                if (transition.previousMoverStateCaptured)
                {
                    try
                    {
                        GameplayWorld gameplayWorld =
                            levelScene.GetComponent<LevelGameWorldComponent>().GameplayWorld;
                        KinematicMoverManager moverManager = gameplayWorld.GetComponent<KinematicMoverManager>();
                        if (moverManager != null)
                        {
                            moverManager.Enable = transition.previousMoverEnabled;
                        }
                    }
                    catch (Exception exception)
                    {
                        cleanupExceptions.Add(exception);
                    }
                }

                if (transition.previousCameraTargetCaptured)
                {
                    try
                    {
                        CameraComponent cameraComponent = levelScene.RootScene().GetComponent<CameraComponent>();
                        cameraComponent?.SetTarget(transition.previousCameraTarget);
                    }
                    catch (Exception exception)
                    {
                        cleanupExceptions.Add(exception);
                    }
                }

                if (transition.createdUnit != null && !transition.createdUnit.IsDisposed)
                {
                    try
                    {
                        transition.createdUnit.Dispose();
                    }
                    catch (Exception exception)
                    {
                        cleanupExceptions.Add(exception);
                    }
                }

                if (transition.previousObserveUnitIdCaptured)
                {
                    try
                    {
                        GameplayWorld gameplayWorld =
                            levelScene.GetComponent<LevelGameWorldComponent>().GameplayWorld;
                        UnitManager unitManager = gameplayWorld.GetComponent<UnitManager>();
                        if (unitManager == null)
                        {
                            throw new System.InvalidOperationException("The GameplayWorld has no UnitManager.");
                        }

                        unitManager.ObserveUnitId = transition.previousObserveUnitId;
                    }
                    catch (Exception exception)
                    {
                        cleanupExceptions.Add(exception);
                    }
                }

                try
                {
                    transition.restoreLightingSettings();
                }
                catch (Exception exception)
                {
                    cleanupExceptions.Add(exception);
                }
            }

            ThrowCleanupExceptions(cleanupExceptions);
            await ETTask.CompletedTask;
        }

        private static void ThrowCleanupExceptions(List<Exception> cleanupExceptions)
        {
            if (cleanupExceptions.Count == 1)
            {
                ExceptionDispatchInfo.Capture(cleanupExceptions[0]).Throw();
            }
            else if (cleanupExceptions.Count > 1)
            {
                throw new AggregateException("Client LevelScene cleanup failed.", cleanupExceptions);
            }
        }
    }

    /// <summary>
    /// Unloads the previous Unity scene after LevelScene enabling has succeeded.
    /// </summary>
    [Event(ELogicSceneType.Client)]
    public class EventHandler_AfterEnableLevelScene_Client : AEvent<LogicScene, Event_AfterEnableLevelScene>
    {
        protected override async ETTask Run(LogicScene clientScene, Event_AfterEnableLevelScene eventInfo)
        {
            try
            {
                LogicScene levelScene = clientScene.GetComponent<CurrentSceneComponent>()?.CurrentScene;
                if (levelScene == null)
                {
                    throw new System.InvalidOperationException("The enabled LevelScene is unavailable for cleanup.");
                }

                LevelSceneViewTransitionComponent transition =
                    levelScene.GetComponent<LevelSceneViewTransitionComponent>();
                if (transition == null)
                {
                    throw new System.InvalidOperationException("The enabled LevelScene has no transition state.");
                }

                Scene previousActiveScene = transition.previousActiveScene;
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    ResourcesLoaderComponent resourcesLoader = clientScene.GetComponent<ResourcesLoaderComponent>();
                    if (resourcesLoader == null)
                    {
                        throw new System.InvalidOperationException(
                            "The client scene has no resource loader for previous-scene cleanup.");
                    }

                    await resourcesLoader.UnloadSceneAsync(previousActiveScene);
                }
            }
            catch (System.Exception exception)
            {
                Log.Error(exception);
            }
        }
    }
}
