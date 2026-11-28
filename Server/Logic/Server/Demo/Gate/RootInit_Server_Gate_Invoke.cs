using Bliss.Core.EventSystem;
using Bliss.Id;
using Bliss.Server;
using Bliss.Timer;
using ET;

namespace Bliss
{
    /// <summary>
    /// Gate初始化脚本
    /// </summary>
    [Invoke((long)ELogicSceneType.Gate)]
    public class RootInit_Server_Gate_Invoke : AInvokeHandler<Invoke_RootInit, ETTask>
    {
        /// <summary>Initializes Gate services and its login transit GameplayWorld.</summary>
        /// <param name="args">The Root initialization request.</param>
        public override async ETTask Handle(Invoke_RootInit args)
        {
            args.ThrowIfCancellationRequested();

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
            rootScene.AddComponent<GateSessionKeyComponent>();

            GameplayWorldFactory.Create(rootScene, GameplayWorldFactory.DEFAULT_WORLD_ID,
                IdGenerator.Instance.GenerateInstanceId(), GameplayWorldFactory.DEFAULT_WORLD_NAME);
            
            var sceneCfg = ConfigManager.Instance.Tables.TbServerScene.Get(args.Root.Id);
            var netCmp = rootScene.AddComponent<NetworkComponent, NetworkServerParam>(new NetworkServerParam()
            {
                BindEndPoint = sceneCfg.OuterIpOuterPort,
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });
            Log.Info($"Bind Gate Address: {sceneCfg.OuterIpOuterPort}");
            
            await ETTask.CompletedTask;
        }
    }
}