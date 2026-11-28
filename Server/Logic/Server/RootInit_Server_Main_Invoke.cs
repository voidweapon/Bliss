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
            if (Options.Instance.InternalMessageForwardTimeoutMS <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Options.InternalMessageForwardTimeoutMS),
                    Options.Instance.InternalMessageForwardTimeoutMS,
                    "Internal message forwarding timeout must be greater than zero.");
            }

            LogicScene rootScene = args.Root.Scene;
            
            World.Instance.AddSingletonNoInit<ProcessMessageManager>();
            rootScene.AddComponent<NetworkEntityManagerComponent>();
            rootScene.AddComponent<TimerComponent>();
            rootScene.AddComponent<ProcessInnerMessageProxy>();
            rootScene.AddComponent<MessageSender>();
            rootScene.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.UnOrderedMessage);
            
            var processCfg = ConfigManager.Instance.Tables.TbServerProcess.Get(args.Root.Process);
            
            rootScene.AddComponent<NetworkComponent, NetworkServerParam>(new NetworkServerParam()
            {
                BindEndPoint = processCfg.InnerIpInnerPort,
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });
            Log.Info($"Bind Inner Address: {processCfg.InnerIpInnerPort}");


            GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Server();

            await ETTask.CompletedTask;
        }
    }

    /// <summary>Initializes the configured Server Roots after Main has become ready.</summary>
    [Invoke((long)ELogicSceneType.Main)]
    public class RootInit_Server_ConfiguredRoots_Invoke : AInvokeHandler<Invoke_CreateConfiguredServerRoots, ETTask>
    {
        /// <inheritdoc />
        public override async ETTask Handle(Invoke_CreateConfiguredServerRoots args)
        {
            foreach (var sceneCfg in ConfigManager.Instance.Tables.TbServerScene.DataList)
            {
                if (sceneCfg.Process == Options.Instance.Process)
                {
                    await RootManager.Instance.Create(sceneCfg.Id, sceneCfg.Zone,
                        Enum.Parse<ELogicSceneType>(sceneCfg.SceneType), sceneCfg.Name, useInitializationGate: true);
                }
            }
            
            await ETTask.CompletedTask;
        }
    }
}
