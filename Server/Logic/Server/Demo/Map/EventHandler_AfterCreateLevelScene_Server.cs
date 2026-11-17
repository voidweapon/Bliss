using System;
using Bliss.Base.Collections;
using Bliss.Physics;
using ET;

namespace Bliss;

/// <summary>
/// Creates and loads the physical resources required by a LevelScene.
/// </summary>
[Event(ELogicSceneType.Level)]
public class EventHandler_AfterCreateLevelScene_Server : AEvent<LogicScene, Event_AfterCreateLevelScene>
{
    protected override async ETTask Run(LogicScene levelScene, Event_AfterCreateLevelScene eventInfo)
    {
        LogicScene mapScene = levelScene.RootScene();
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
        
        await ETTask.CompletedTask;
    }
}