using System.IO;
using Bliss.Physics;
using ET;

namespace Bliss;

/// <summary>
/// Starts server-side LevelScene behavior after all required resources are prepared.
/// </summary>
[Event(ELogicSceneType.Level)]
public class EventHandler_EnableLevelScene_Server : AEvent<LogicScene, Event_EnableLevelScene>
{
    protected override async ETTask Run(LogicScene levelScene, Event_EnableLevelScene eventInfo)
    {
        LevelGameWorldComponent levelAssociation = levelScene.GetComponent<LevelGameWorldComponent>();
        GameplayWorld gameplayWorld = levelAssociation.GameplayWorld;
        LevelConfig levelConfig = ConfigManager.Instance.Tables.TbLevelConfig.GetOrDefault(levelAssociation.LevelConfigId);
        LevelSceneServerEnableStateComponent enableState =
            levelScene.AddComponent<LevelSceneServerEnableStateComponent>();

        levelScene.GetComponent<PhysicalScene>().StartUpdates();

        string settingsFileName = Path.GetFullPath($"../ServerRes/SceneSettings/{levelConfig.Name}.bytes");
        LogicSceneFactory.LoadLogicSceneActorFromSettingFile(
            levelScene,
            settingsFileName,
            ENetRole.Authority,
            ENetRole.SimulatedProxy);

        if (gameplayWorld.GetComponent<DemoGameMode>() == null)
        {
            enableState.CreatedDemoGameMode = gameplayWorld.AddComponent<DemoGameMode>();
        }

        await ETTask.CompletedTask;
    }
}
