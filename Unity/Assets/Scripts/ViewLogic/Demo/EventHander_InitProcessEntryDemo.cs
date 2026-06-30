using System.Net.Sockets;
using Bliss.BootProcess;
using Bliss.Debugger.GameDebugStats;
using Bliss.Id;
using Bliss.Physics;
using Bliss.Timer;
using Bliss.ViewModel;
using Bliss.ViewModel.Debugger;
using ET;

namespace Bliss
{
    [Event(ELogicSceneType.Main)]
    public class EventHander_InitProcessEntryDemo : AEvent<LogicScene, Event_InitProcessEntry_Demo>
    {
        protected override async ETTask Run(LogicScene mainScene, Event_InitProcessEntry_Demo eventInfo)
        {
            mainScene.AddComponent<ViewGlobalComponent>();
            mainScene.AddComponent<ResourcesLoaderComponent>();
            mainScene.AddComponent<CameraComponent>();
            mainScene.AddComponent<UIGlobalComponent>();
            
            mainScene.AddComponent<TransformStatDebugComponent>();
            World.Instance.AddSingleton<GameDebugStates>();
            World.Instance.AddSingleton<GameDebugStatesView>();
            
            var clientScene = LogicSceneFactoryCore.CreateLogicScene(mainScene, 1, IdGenerator.Instance.GenerateInstanceId(), ELogicSceneType.Client, "Client");
            clientScene.AddComponent<ResourcesLoaderComponent>();
            clientScene.AddComponent<NetStatDebugComponent>();
            clientScene.AddComponent<NetworkComponent, NetworkClientParam>(new NetworkClientParam()
            {
                AddressFamily = AddressFamily.InterNetwork,
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });
            
            clientScene.AddComponent<ObjectWait>();
            clientScene.AddComponent<TimerComponent>();
            clientScene.AddComponent<CoroutineLockComponent>();
            World.Instance.AddSingleton<Console, LogicScene>(clientScene);
            
            GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Client();

            var uiComponent = clientScene.AddComponent<UIComponent>();
            await uiComponent.Create(UIType.Login, EUILayer.Mid);
            
            //通知关闭Loading界面
            await EventSystem.Instance.PushAsync(mainScene, new Event_BootProcessFinish());
            
            
            await ETTask.CompletedTask;
        }
    }
}