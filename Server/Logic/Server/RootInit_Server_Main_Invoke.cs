using System;
using Bliss.Core.EventSystem;
using Bliss.Physics;
using Bliss.Server;
using Bliss.Timer;
using ET;

namespace Bliss
{

    [Invoke((long)ELogicSceneType.Main)]
    public class RootInit_Server_Main_Invoke : AInvokeHandler<Invoke_RootInit, ETTask>
    {
        public override async ETTask Handle(Invoke_RootInit args)
        {
            LogicScene rootScene = args.Root.Scene;
            
            World.Instance.AddSingletonNoInit<ProcessMessageManager>();
            rootScene.AddComponent<NetworkEntityManagerComponent>();
            rootScene.AddComponent<TimerComponent>();
            rootScene.AddComponent<ProcessInnerMessageProxy>();
            
            var processCfg = ConfigManager.Instance.Tables.TbServerProcess.Get(args.Root.Process);
            
            rootScene.AddComponent<NetworkComponent, NetworkServerParam>(new NetworkServerParam()
            {
                BindEndPoint = processCfg.InnerIpInnerPort,
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });
            Log.Info($"Bind Inner Address: {processCfg.InnerIpInnerPort}");


            GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Server();
            
            foreach (var sceneCfg in ConfigManager.Instance.Tables.TbServerScene.DataList)
            {
                if (sceneCfg.Process == Options.Instance.Process)
                {
                    await RootManager.Instance.Create(sceneCfg.Id, sceneCfg.Zone, Enum.Parse<ELogicSceneType>(sceneCfg.SceneType), sceneCfg.Name);
                }
            }
            
            Log.Info($"==============Server Init Success==============");
            await ETTask.CompletedTask;
        }
    }
}

