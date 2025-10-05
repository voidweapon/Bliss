using Bliss.Core.EventSystem;
using Bliss.Server;
using ET;

namespace Bliss
{
    [Invoke((long)ELogicSceneType.Main)]
    public class RootInit_Server_Main_Invoke : AInvokeHandlerHandler<Invoke_RootInit, ETTask>
    {
        public override async ETTask Handle(Invoke_RootInit args)
        {
            LogicScene rootScene = args.Root.Scene;
            
            var netCmp = rootScene.AddComponent<NetworkComponent, NetworkServerParam>(new NetworkServerParam()
            {
                BindEndPoint = NetworkHelper.ToIPEndPoint("127.0.0.1", 9001),
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });
            Log.Debug($"Bind Address:127.0.0.1:9001");
            
            await ETTask.CompletedTask;
        }
    }
}

