 using System.Net.Sockets;
using Bliss.Core.EventSystem;
using ET;

namespace Bliss.Logic
{
    [Invoke((long)ELogicSceneType.Main)]
    public class RootInit_Client_Main_Invoke : AInvokeHandlerHandler<Invoke_RootInit, ETTask>
    {
        public override async ETTask Handle(Invoke_RootInit args)
        {
            LogicScene rootScene = args.Root.Scene;
            
            var netCmp = rootScene.AddComponent<NetworkComponent, NetworkClientParam>(new NetworkClientParam()
            {
                AddressFamily = AddressFamily.InterNetwork,
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });
            
            Log.Debug($"Connect to Server:  127.0.0.1:9001");
            var session = netCmp.CreateConnection(NetworkHelper.ToIPEndPoint("127.0.0.1", 9001));

            var skBufferRef =  netCmp.NetDriver.RentBuffer(100);
            var skBuf = skBufferRef.Target;
            skBuf.Reserve(sizeof(int));
            skBuf.Push(sizeof(int))
                .WriteInt(55);
            session.SendRaw(skBufferRef);
        }
    }
}