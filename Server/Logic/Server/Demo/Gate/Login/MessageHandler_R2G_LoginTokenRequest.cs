using ET;

namespace Bliss.Demo.Login;

/// <summary>
/// 网关服务器收到登录请求后，返回一个登录令牌（Token）给客户端，客户端使用这个令牌来进行后续的认证和连接。
/// </summary>
[MessageHandler(ELogicSceneType.Gate)]
public class MessageHandler_R2G_LoginTokenRequest  : MessageHandler<LogicScene, R2G_LoginTokenRequest, G2R_LoginTokenResponse>
{
    protected override async ETTask Run(LogicScene mapScene, R2G_LoginTokenRequest message, G2R_LoginTokenResponse response)
    {
        var gateSessionCmp = mapScene.GetComponent<GateSessionKeyComponent>();
        response.Token = gateSessionCmp.Add(message.Account);
        await ETTask.CompletedTask;
    }
}