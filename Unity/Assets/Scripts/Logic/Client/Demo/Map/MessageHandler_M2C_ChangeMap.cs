using Bliss.CharacterMovement;
using Bliss.Id;
using ET;

namespace Bliss
{
    [MessageHandler(ELogicSceneType.Client)]
    public class MessageHandler_M2C_ChangeMap : MessageHandler<LogicScene, M2C_ChangeMap>
    {
        protected override async ETTask Run(LogicScene clientScene, M2C_ChangeMap message)
        {
            Log.Info($"进入地图 M2C_ChangeMap ");

            LogicScene gameplayWorld = clientScene.GetChild<LogicScene>(1);
            if (message.IsLogin)
            {
                gameplayWorld = LogicSceneFactoryCore.CreateLogicScene(clientScene, 1, IdGenerator.Instance.GenerateInstanceId(), ELogicSceneType.GameplayWorld, "GameplayWorld");
                gameplayWorld.AddComponent<UnitManager>();
                gameplayWorld.AddComponent<BulletManager>();
                gameplayWorld.AddComponent<SceneActorManager>();
                gameplayWorld.AddComponent<KinematicMoverManager, KinematicMoverManagerSettings>(new KinematicMoverManagerSettings()
                {
                    AutoSimulation = true,
                    Interpolate = true,
                    InterpolateMotor = true,
                    InterpolateMover = true,
                });
                await EventSystem.Instance.PushAsync(gameplayWorld, new Event_AfterCreateGameplayWorld());
                
                var currentSceneCmp = clientScene.AddComponent<CurrentSceneComponent>();
                currentSceneCmp.GameplayWorld = gameplayWorld;
            }
            
            await MapHelper.ChangeMap(clientScene, gameplayWorld, message.LevelConfigId, message.LevelId);
        }
    }
}