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
    /// 场景切换流程2
    /// 先切换到空场景(Loading)来卸载, 然后切换到目标场景
    /// 以此来削减内存峰值
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
            
            var loadingHandler = await resourcesLoaderCmp.LoadSceneAsync("Assets/Res/Scenes/Loading.unity", LoadSceneMode.Single);
            var levelHandler = await resourcesLoaderCmp.LoadSceneAsync($"Assets/Res/Scenes/{levelConfig.Name}.unity", LoadSceneMode.Additive);
            levelHandler.ActivateScene();
            
            
            loadingHandler.UnloadAsync();

            var clientScene = levelScene.GetComponent<LevelGameWorldComponent>().GameplayWorld.Parent;
            clientScene.GetComponent<ObjectWait>().Notify(new Wait_LoadLevelSceneView(), true);
           await ETTask.CompletedTask;
        }
    }
}