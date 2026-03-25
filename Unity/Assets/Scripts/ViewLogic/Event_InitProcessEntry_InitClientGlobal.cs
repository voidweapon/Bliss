using System.Net.Sockets;
using Bliss.Core.Network;
using Bliss.Debugger.GameDebugStats;
using Bliss.Id;
using Bliss.Timer;
using Bliss.ViewLogic.Debugger;
using Bliss.ViewModel;
using Bliss.ViewModel.Debugger;
using ET;
using Unity.Mathematics;

namespace Bliss.ViewLogic
{
    [Event(ELogicSceneType.Main)]
    public class Event_InitProcessEntry_InitClientGlobal : AEvent<LogicScene, Event_InitProcessEntry>
    {
        protected override async ETTask Run(LogicScene mainScene, Event_InitProcessEntry eventInitProcessInfo)
        {
            mainScene.AddComponent<ViewGlobalComponent>();
            mainScene.AddComponent<ResourcesLoaderComponent>();
            mainScene.AddComponent<TransformStatDebugComponent>();
            World.Instance.AddSingleton<GameDebugStates>();
            World.Instance.AddSingleton<GameDebugStatesView>();
            
            var clientScene = LogicSceneFactory.CreateLogicScene(mainScene, 0, IdGenerator.Instance.GenerateInstanceId(), ELogicSceneType.Client, "LocalClient");
            World.Instance.AddSingleton<Console, LogicScene>(clientScene);
            clientScene.AddComponent<ResourcesLoaderComponent>();
            clientScene.AddComponent<NetStatDebugComponent>();
            clientScene.AddComponent<LevelNetworkObjectCollectionComponent>();
            clientScene.AddComponent<NetworkComponent, NetworkClientParam>(new NetworkClientParam()
            {
                AddressFamily = AddressFamily.InterNetwork,
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });
            
            var remoteClientScene = LogicSceneFactory.CreateLogicScene(mainScene, 1, IdGenerator.Instance.GenerateInstanceId(), ELogicSceneType.Client, "RemoteClient");
            remoteClientScene.AddComponent<ResourcesLoaderComponent>();
            remoteClientScene.AddComponent<LevelNetworkObjectCollectionComponent>();
            remoteClientScene.AddComponent<NetworkComponent, NetworkClientParam>(new NetworkClientParam()
            {
                AddressFamily = AddressFamily.InterNetwork,
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });
            
            
            var serverScene = LogicSceneFactory.CreateLogicScene(mainScene, 2, IdGenerator.Instance.GenerateInstanceId(), ELogicSceneType.Client, "Server");
            serverScene.AddComponent<ResourcesLoaderComponent>();
            serverScene.AddComponent<LevelPlayerMgrComponent>();
            serverScene.AddComponent<LevelNetworkObjectCollectionComponent>();
            serverScene.AddComponent<NetworkComponent, NetworkServerParam>(new NetworkServerParam()
            {
                BindEndPoint = NetworkHelper.ToIPEndPoint("127.0.0.1", 9100),
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });

            var networkSimulateSetting = new NetworkSimulationSettings()
            {
                Latency = 1000,
                LatencyVariance = 2,
                // LatencyMin = 100,
                // Jitter = 25,
            };
            clientScene.GetComponent<NetworkComponent>().NetDriver.SetNetSimulationSettings(networkSimulateSetting);
            remoteClientScene.GetComponent<NetworkComponent>().NetDriver.SetNetSimulationSettings(networkSimulateSetting);
            // serverScene.GetComponent<NetworkComponent>().NetDriver.SetNetSimulationSettings(networkSimulateSetting);
            
            await mainScene.GetComponent<TimerComponent>().WaitDurationAsync(1000);

            //Testaa.TestData = 5666;
            
            var sessionLocalClient = clientScene.GetComponent<NetworkComponent>().CreateConnection(NetworkHelper.ToIPEndPoint("127.0.0.1", 9100));
            var pingComponent = sessionLocalClient.AddComponent<PingComponent>();
            pingComponent.IsUpdateServerTime = true;
            clientScene.AddComponent<SessionComponent>().Session = sessionLocalClient;
            
            var sessionRemoteClient = remoteClientScene.GetComponent<NetworkComponent>().CreateConnection(NetworkHelper.ToIPEndPoint("127.0.0.1", 9100));
            sessionRemoteClient.AddComponent<PingComponent>();
            remoteClientScene.AddComponent<SessionComponent>().Session = sessionRemoteClient;
            
            await EventSystem.Instance.PushAsync(clientScene, new Event_InitProcessAppInitFinish());

            using var enterLevelMsg = C2G_EnterLevel.Create(true);
            _ = await sessionLocalClient.Call(enterLevelMsg, new ETCancellationToken());
            _ = await sessionRemoteClient.Call(enterLevelMsg, new ETCancellationToken());
            
            var viewPath = "Assets/Res/Prefabs/TestActor.prefab";
            var clientActor0 = await ActorCreateHelper.CreateSceneActor(clientScene, 1, ENetRole.SimulatedProxy, ENetRole.Authority, true, viewPath);
            var serverActor0 = await ActorCreateHelper.CreateSceneActor(serverScene, 1, ENetRole.Authority, ENetRole.SimulatedProxy, true, viewPath);
            
            mainScene.GetComponent<TransformStatDebugComponent>().RegisterWatchTransform(0, clientActor0.GetComponent<GameObjectViewComponent>().View.transform);
            mainScene.GetComponent<TransformStatDebugComponent>().RegisterWatchTransform(2, serverActor0.GetComponent<GameObjectViewComponent>().View.transform);
            mainScene.GetComponent<TransformStatDebugComponent>().RegisterWatchCmp(0, serverActor0.GetComponent<PeriodicMotionComponent>(), clientActor0.GetComponent<PeriodicMotionComponent>());
        }
    }
}