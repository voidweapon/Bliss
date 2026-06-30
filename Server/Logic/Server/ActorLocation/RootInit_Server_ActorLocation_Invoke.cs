using Bliss.Core.EventSystem;
using Bliss.Server;
using Bliss.Timer;
using ET;

namespace Bliss
{
    [Invoke((long)ELogicSceneType.Location)]
    public class RootInit_Server_ActorLocation_Invoke : AInvokeHandler<Invoke_RootInit, ETTask>
    {
        public override async ETTask Handle(Invoke_RootInit args)
        {
            LogicScene rootScene = args.Root.Scene;

            rootScene.AddComponent<TimerComponent>();
            rootScene.AddComponent<CoroutineLockComponent>();
            rootScene.AddComponent<ActorNetworkLocationManager>();
            rootScene.AddComponent<NetworkEntityManagerComponent>();
            rootScene.AddComponent<ProcessInnerMessageProxy>();
            rootScene.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.UnOrderedMessage);

            
            await ETTask.CompletedTask;
        }
    }
}