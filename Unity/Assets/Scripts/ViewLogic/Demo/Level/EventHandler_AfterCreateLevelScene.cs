using Bliss.ViewModel;
using ET;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bliss
{
    /// <summary>
    /// Preloads client resources for a LevelScene without activating its Unity scene.
    /// <seealso cref="MapHelper.ChangeMap"/>
    /// </summary>
    [Event(ELogicSceneType.Level)]
    public class EventHandler_AfterCreateLevelScene : AEvent<LogicScene, Event_AfterCreateLevelScene>
    {
        protected override async ETTask Run(LogicScene levelScene, Event_AfterCreateLevelScene eventInfo)
        {
            levelScene.AddComponent<UIComponent>();
            ResourcesLoaderComponent resourcesLoader = levelScene.AddComponent<ResourcesLoaderComponent>();
            LevelConfig levelConfig = ConfigManager.Instance.Tables.TbLevelConfig.GetOrDefault(eventInfo.LevelConfigId);

            LevelSceneViewTransitionComponent transition = levelScene.AddComponent<LevelSceneViewTransitionComponent>();
            transition.previousActiveScene = SceneManager.GetActiveScene();
            var sceneHandle = await resourcesLoader.LoadSceneAsync(
                $"Assets/Res/Scenes/{levelConfig.Name}.unity",
                LoadSceneMode.Additive);
            transition.destinationScene = sceneHandle.SceneObject;
        }
    }

    /// <summary>
    /// Retains client-only scene transition state until enabling and cleanup finish.
    /// </summary>
    public class LevelSceneViewTransitionComponent : Entity
    {
        internal Scene previousActiveScene;
        internal Scene destinationScene;
        internal bool previousMoverStateCaptured;
        internal bool previousMoverEnabled;
        internal bool previousCameraTargetCaptured;
        internal EntityRef<Entity> previousCameraTarget;
        internal Unit createdUnit;
        internal bool previousObserveUnitIdCaptured;
        internal long previousObserveUnitId;
        internal bool previousLightingSettingsCaptured;
        internal LightmapData[] previousLightmaps;
        internal LightmapsMode previousLightmapsMode;
        internal LightProbes previousLightProbes;
        internal SphericalHarmonicsL2[] previousBakedProbes;

        internal void captureLightingSettings()
        {
            this.previousLightmapsMode = LightmapSettings.lightmapsMode;
            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            this.previousLightmaps = lightmaps == null ? null : (LightmapData[])lightmaps.Clone();
            this.previousLightProbes = LightmapSettings.lightProbes;
            this.previousBakedProbes = this.previousLightProbes == null
                ? null
                : (SphericalHarmonicsL2[])this.previousLightProbes.bakedProbes.Clone();
            this.previousLightingSettingsCaptured = true;
        }

        internal void restoreLightingSettings()
        {
            if (!this.previousLightingSettingsCaptured)
            {
                return;
            }

            List<Exception> cleanupExceptions = new List<Exception>();
            try
            {
                LightmapSettings.lightmapsMode = this.previousLightmapsMode;
            }
            catch (Exception exception)
            {
                cleanupExceptions.Add(exception);
            }

            try
            {
                LightmapSettings.lightmaps = this.previousLightmaps;
            }
            catch (Exception exception)
            {
                cleanupExceptions.Add(exception);
            }

            try
            {
                LightmapSettings.lightProbes = this.previousLightProbes;
            }
            catch (Exception exception)
            {
                cleanupExceptions.Add(exception);
            }

            if (this.previousLightProbes != null && this.previousBakedProbes != null)
            {
                try
                {
                    this.previousLightProbes.bakedProbes = this.previousBakedProbes;
                }
                catch (Exception exception)
                {
                    cleanupExceptions.Add(exception);
                }
            }

            if (cleanupExceptions.Count == 0)
            {
                this.previousLightingSettingsCaptured = false;
            }
            else if (cleanupExceptions.Count == 1)
            {
                ExceptionDispatchInfo.Capture(cleanupExceptions[0]).Throw();
            }
            else
            {
                throw new AggregateException("Restoring client lighting settings failed.", cleanupExceptions);
            }
        }
    }
}
