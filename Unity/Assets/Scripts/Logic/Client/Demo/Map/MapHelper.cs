using ET;

namespace Bliss
{
    public  static class MapHelper
    {
        public static async ETTask EnterMap(LogicScene clientScene, int mapId, ETCancellationToken ctx)
        {
            Session session = (Session)clientScene.GetComponent<ClientSessionComponent>().Session;
            if (session == null)
            {
                return;
            }
            
            var g2cEnterMap = await session.Call(new C2G_EnterMap()
            {
                MapId = mapId,
            }, ctx) as G2C_EnterMap;
            
            if (g2cEnterMap == null || g2cEnterMap.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"进入地图失败: {g2cEnterMap.Error}");
                
                clientScene.GetComponent<ObjectWait>().Notify(new Wait_CreateMyUnit() { Error = WaitTypeError.Cancel });
                clientScene.GetComponent<ObjectWait>().Notify(new Wait_LoadLevelSceneView() { Error = WaitTypeError.Cancel });
                clientScene.GetComponent<ObjectWait>().Notify(new Wait_CreateLevelScene() { Error = WaitTypeError.Cancel });
                
                var currentSceneComponent = clientScene.GetComponent<CurrentSceneComponent>();
                currentSceneComponent?.CurrentScene?.Dispose();
                
                return;
            }
            
            //等待场景切换完成信号
            var waitChangeScene = await clientScene.GetComponent<ObjectWait>().Wait<Wait_FinishChangeLevelScene>();
            if (waitChangeScene.Error != WaitTypeError.Success)
            {
                return;
            }
            Log.Info($"进入地图{mapId}成功");

        }

        public static async ETTask ChangeMap(LogicScene clientScene, LogicScene gameplayWorld, int levelConfigId, long levelId)
        {
            var waitChangeLevelViewTcs = clientScene.GetComponent<ObjectWait>().Wait<Wait_LoadLevelSceneView>();
            var waitCreateMyUnitTcs =  clientScene.GetComponent<ObjectWait>().Wait<Wait_CreateMyUnit>();

            await EventSystem.Instance.PushAsync(clientScene, new Event_StartChangeLevelScene());
            var currentSceneComponent = clientScene.GetComponent<CurrentSceneComponent>();
            currentSceneComponent.CurrentScene?.Dispose();
            
            var unitManager = gameplayWorld.GetComponent<UnitManager>();
            var bulletManager = gameplayWorld.GetComponent<BulletManager>();
            var sceneActorManager = gameplayWorld.GetComponent<SceneActorManager>();
            
            //Create Level Scene
            LevelConfig levelConfig = ConfigManager.Instance.Tables.TbLevelConfig.GetOrDefault(levelConfigId);
            var levelScene = await LevelSceneFactory.Create(gameplayWorld, gameplayWorld, levelConfigId, levelId, levelConfig.Name);
            currentSceneComponent.CurrentScene = levelScene;
            clientScene.GetComponent<ObjectWait>().Notify(new Wait_CreateLevelScene());
            
            
            //等待CreateMyUnit的消息
            var waitCreateMyUnit = await waitCreateMyUnitTcs;
            if (waitCreateMyUnit.Error != WaitTypeError.Success)
            {
                return;
            }
            
            M2C_CreateMyUnit m2CCreateMyUnit = waitCreateMyUnit.Message;
            //从服务过来的NetRole,在本地需要对调一下
            (m2CCreateMyUnit.Unit.LocalRole, m2CCreateMyUnit.Unit.RemoteRole) = (m2CCreateMyUnit.Unit.RemoteRole, m2CCreateMyUnit.Unit.LocalRole);
            Unit myUnit = m2CCreateMyUnit.Unit.CreateUnit(gameplayWorld);
            unitManager.ObserveUnitId = myUnit.Id;
            
            await EventSystem.Instance.PushAsync(levelScene, new Event_EndCreateLevelScene()
            {
                LevelConfigId = levelConfigId,
            });
            
            var waitChangeLevelView = await waitChangeLevelViewTcs;
            if (waitChangeLevelView.Error != WaitTypeError.Success)
            {
                return;
            }

            
            await EventSystem.Instance.PushAsync(clientScene, new Event_EndChangeLevelScene());
            
            //通知等待场景切换的协程
            clientScene.GetComponent<ObjectWait>().Notify(new Wait_FinishChangeLevelScene());
        }

    }
}