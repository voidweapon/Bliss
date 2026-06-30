  using System.Net.Sockets;
using Bliss.Core.EventSystem;
using Bliss.Timer;
using ET;

namespace Bliss.Logic
{
    [Invoke((long)ELogicSceneType.Main)]
    public class InovkeHandler_RootInit_Main_Client : AInvokeHandler<Invoke_RootInit, ETTask>
    {
        public override async ETTask Handle(Invoke_RootInit args)
        {
            LogicScene rootScene = args.Root.Scene;
            //处理一些MainScene上的初始化
            rootScene.AddComponent<TimerComponent>();
            rootScene.AddComponent<CoroutineLockComponent>();
            rootScene.AddComponent<NetworkEntityManagerComponent>();
            rootScene.AddComponent<ObjectWait>();

            //await EventSystem.Instance.PushAsync(rootScene, new Event_InitProcessEntry());
            await EventSystem.Instance.PushAsync(rootScene, new Event_InitProcessEntry_Demo());
        }
    }
}