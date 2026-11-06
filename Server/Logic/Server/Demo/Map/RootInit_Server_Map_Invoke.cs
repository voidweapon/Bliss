using Bliss.Core.EventSystem;
using Bliss.Id;
using Bliss.Server;
using Bliss.Timer;
using ET;

namespace Bliss
{
    [Invoke((long)ELogicSceneType.Map)]
    public class RootInit_Server_Map_Invoke : AInvokeHandler<Invoke_RootInit, ETTask>
    {
        public override async ETTask Handle(Invoke_RootInit args)
        {
            Log.Info($"Init Map Start");
            
            LogicScene rootScene = args.Root.Scene;
            rootScene.AddComponent<TimerComponent>();
            rootScene.AddComponent<CoroutineLockComponent>();
            rootScene.AddComponent<NetworkEntityManagerComponent>();
            rootScene.AddComponent<ProcessInnerMessageProxy>();
            rootScene.AddComponent<ActorNetworkLocationProxyComponent>();
            rootScene.AddComponent<MessageSender>();
            rootScene.AddComponent<MessageLocationSenderManager>();
            rootScene.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.UnOrderedMessage);
            rootScene.AddComponent<PlayerManagerComponent>();

            
            Log.Info($"Init Map: PhysicalWorldComponent");
            rootScene.AddComponent<PhysicalWorldComponent>();
            
            var gameplayWorld = LogicSceneFactoryCore.CreateLogicScene(rootScene, 1, IdGenerator.Instance.GenerateInstanceId(), ELogicSceneType.GameplayWorld, "GameplayWorld");
            gameplayWorld.AddComponent<UnitManager>();
            gameplayWorld.AddComponent<BulletManager>();
            gameplayWorld.AddComponent<SceneActorManager>();
            gameplayWorld.AddComponent<AoiManagerComponent>();
            gameplayWorld.AddComponent<ReplicationGraph>();
            
            
            var levelConfigList = ConfigManager.Instance.Tables.TbLevelConfig.DicSceneId2Level[(int)rootScene.Id];
            var defaultLevelConfig = levelConfigList[0];
            await LevelSceneFactory.Create(gameplayWorld, gameplayWorld, defaultLevelConfig.Id, defaultLevelConfig.Id, defaultLevelConfig.Name);
            
            await ETTask.CompletedTask;
        }
    }
}