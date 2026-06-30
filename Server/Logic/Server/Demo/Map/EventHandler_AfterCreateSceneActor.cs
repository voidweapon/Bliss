using ET;

namespace Bliss;

[Event(ELogicSceneType.Level)]
public class EventHandler_AfterCreateSceneActor : AEvent<LogicScene, Event_AfterCreateSceneActor>
{
    protected override async ETTask Run(LogicScene levelScene, Event_AfterCreateSceneActor eventInfo)
    {
        var actor = (Entity)eventInfo.Actor;
        if (actor is SceneActor sceneActor)
        {
            var playerSpawnPoint = sceneActor.GetComponent<PlayerSpawnPoint>();
            if (playerSpawnPoint != null)
            {
                var playerSpawnPointMgr = levelScene.GetComponent<LevelPlayerSpawnPointMgr>();
                if (playerSpawnPointMgr == null)
                {
                    playerSpawnPointMgr = levelScene.AddComponent<LevelPlayerSpawnPointMgr>();
                }
                
                playerSpawnPointMgr.PlayerSpawnPoints.Add(playerSpawnPoint);
            }
        }
        
        await ETTask.CompletedTask;
    }
}