using Bliss.Timer;
using ET;

namespace Bliss;

/// <summary>
/// 处理登录请求，获取Gate地址和Token后返回给客户端，并在1秒后关闭Session
/// </summary>
[SessionMessageHandler(ELogicSceneType.Realm)]
public class MessageHandler_C2R_Login : SessionMessageHandler<C2R_Login, R2C_Login>
{
    protected override async ETTask Run(Session session, C2R_Login message, R2C_Login response)
    {
        var sceneConfig = RealmGateAddressHelper.GetGate(session.Root().Zone, message.Account);

        var r2g_gateToken = R2G_LoginTokenRequest.Create();
        r2g_gateToken.Account = message.Account;
        
        var g2r_gateToken = (G2R_LoginTokenResponse)await session.RootScene().GetComponent<MessageSender>().Call(sceneConfig.ActorAddress, r2g_gateToken);
        response.GateToken =  g2r_gateToken.Token;
        response.GateAddress = sceneConfig.OuterIpOuterPort.ToString();
        
        CloseSession(session).Coroutine();
    }
    
    private async ETTask CloseSession(Session session)
    {
        await session.RootScene().GetComponent<TimerComponent>().WaitDurationAsync(1000);
        session.Dispose();
    }
}