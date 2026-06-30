using Bliss.Core.EventSystem;
using Bliss.Server;
using Bliss.Timer;
using ET;

namespace Bliss
{

    [Invoke((long)ELogicSceneType.Realm)]
    public class RootInit_Server_Realm_Invoke : AInvokeHandler<Invoke_RootInit, ETTask>
    {
        public override async ETTask Handle(Invoke_RootInit args)
        {
            LogicScene rootScene = args.Root.Scene;

            rootScene.AddComponent<TimerComponent>();
            rootScene.AddComponent<CoroutineLockComponent>();
            rootScene.AddComponent<NetworkEntityManagerComponent>();
            rootScene.AddComponent<ProcessInnerMessageProxy>();
            rootScene.AddComponent<ActorNetworkLocationProxyComponent>();
            rootScene.AddComponent<MessageSender>();
            rootScene.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.UnOrderedMessage);
            rootScene.AddComponent<PlayerManagerComponent>();
            
            
            var sceneCfg = ConfigManager.Instance.Tables.TbServerScene.Get(args.Root.Id);
            var netCmp = rootScene.AddComponent<NetworkComponent, NetworkServerParam>(new NetworkServerParam()
            {
                BindEndPoint = sceneCfg.OuterIpOuterPort,
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });
            Log.Info($"Bind Realm Address: {sceneCfg.OuterIpOuterPort}");
            
            await ETTask.CompletedTask;
        }
    }
}