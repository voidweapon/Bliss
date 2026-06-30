using ET;

namespace Bliss
{
    public static class LoginHelper
    {
        public static async ETTask Login(LogicScene clientScene, string account, string password, ETCancellationToken ctx)
        {
            clientScene.RemoveComponent<ClientSessionComponent>();
            var networkComponent = clientScene.GetComponent<NetworkComponent>();


            R2C_Login r2C_Login = null;
            using(var realmSession = networkComponent.CreateConnection(NetworkHelper.ToIPEndPoint("127.0.0.1", 30300)))
            {
                // 发送登录请求
                 r2C_Login = await realmSession.Call(new C2R_Login() { Account = account, Password = password }, ctx) as R2C_Login;
                if (r2C_Login.Error != ErrorCode.ERR_Success)
                {
                    Log.Error($"登录失败: {r2C_Login.Error}");
                    return;
                }
            }


            var session = networkComponent.CreateConnection(NetworkHelper.ToIPEndPoint(r2C_Login.GateAddress));
            var g2CLogin = await session.Call(new C2G_Login()
            {
                GateToken = r2C_Login.GateToken,
            }, ctx) as G2C_Login;

            if (g2CLogin == null || g2CLogin.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"登录失败: {g2CLogin.Error}");
                session.Dispose();
                return;
            }
            clientScene.AddComponent<ClientSessionComponent, Session>(session);
            clientScene.AddChild<Player, string>(account);
            
            EventSystem.Instance.Push(clientScene, new Event_LoginSuccess());
            Log.Info($"登录成功");
        }
    }
}