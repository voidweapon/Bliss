using System;
using System.IO;
using Bliss.Base.Collections;
using Bliss.Physics;
using ET;

namespace Bliss;

[Event(ELogicSceneType.Level)]
public class EventHandler_AfterCreateLevelScene_Server : AEvent<LogicScene, Event_AfterCreateLevelScene>
{
    protected override async ETTask Run(LogicScene levelScene, Event_AfterCreateLevelScene eventInfo)
    {
        LogicScene mapScene = levelScene.RootScene();
        LogicScene gameplayWorld = levelScene.GetSceneComponent<LevelGameWorldComponent>().GameplayWorld;
        var levelConfig = ConfigManager.Instance.Tables.TbLevelConfig.GetOrDefault(eventInfo.LevelConfigId);
        
        //初始化物理
        var physicalWorldComponent = mapScene.GetComponent<PhysicalWorldComponent>();
        
        var  physicalScene = levelScene.GetComponent<PhysicalScene>();
        if (physicalScene == null)
        { 
            physicalScene = levelScene.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorldComponent);
        }
        
        //加载场景物理信息
        physicalScene.LoadActorFromMemoryPackFile(levelConfig.Name);
        
        
        bool demoGameModeCreated = false;
        levelScene.GetComponent<LevelGameWorldComponent>().DeferUntilCommit(
            () =>
            {
                if (gameplayWorld.GetComponent<DemoGameMode>() == null)
                {
                    gameplayWorld.AddComponent<DemoGameMode>();
                    demoGameModeCreated = true;
                }
            },
            () =>
            {
                if (demoGameModeCreated)
                {
                    gameplayWorld.RemoveComponent<DemoGameMode>();
                    demoGameModeCreated = false;
                }
            });
        
        //加载场景配置对象
        string settingsFileName = Path.GetFullPath($"../ServerRes/SceneSettings/{levelConfig.Name}.bytes");
        LogicSceneFactory.LoadLogicSceneActorFromSettingFile(levelScene, settingsFileName, ENetRole.Authority, ENetRole.SimulatedProxy);


        
        await ETTask.CompletedTask;
    }
}