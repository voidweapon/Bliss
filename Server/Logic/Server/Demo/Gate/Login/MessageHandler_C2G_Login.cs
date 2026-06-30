using ET;

namespace Bliss;

/// <summary>
/// 处理玩家登录请求
/// </summary>
[SessionMessageHandler(ELogicSceneType.Gate)]
public class MessageHandler_C2G_Login : SessionMessageHandler<C2G_Login, G2C_Login>
{
    protected override async ETTask Run(Session session, C2G_Login message, G2C_Login response)
    {
        LogicScene scene = session.RootScene();
        
        var gateSessionCmp =  session.RootScene().GetComponent<GateSessionKeyComponent>();
        var playerAccount = gateSessionCmp.Get(message.GateToken);
        if (playerAccount == null)
        {
            response.Error = ErrorCode.ERR_CONNECT_GATE_KEY_ERROR;
            response.Message = "Gate key验证失败!";
            return;
        }

        var playerMgr = scene.GetComponent<PlayerManagerComponent>();
        var player = playerMgr.Find(playerAccount);
        
        if (player == null)
        {
            //首次登陆
            player = playerMgr.Add(playerAccount);
            player.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.UnOrderedMessage);
            var playerSessionCmp = player.AddComponent<PlayerSessionComponent>();
            playerSessionCmp.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.GateSession);
            var sessionPlayerCmp = session.AddComponent<SessionPlayerComponent>();

            playerSessionCmp.Session = session;
            sessionPlayerCmp.Player = player;
            await playerSessionCmp.AddNetworkLocation((int)ENetworkLocationCategory.GateSession);
            await player.AddNetworkLocation((int)ENetworkLocationCategory.Player);
        }
        else
        {
            //已经登陆
            PlayerSessionComponent playerSessionComponent = player.GetComponent<PlayerSessionComponent>();
            if (playerSessionComponent.Session != null)
            {
                var oldSessionPlayerCmp = session.GetComponent<SessionPlayerComponent>();
                oldSessionPlayerCmp.DestroyReason = 1;
                playerSessionComponent.Session.Dispose();
            }
            playerSessionComponent.Session = session;
            
            var sessionPlayerCmp = session.AddComponent<SessionPlayerComponent>();
            sessionPlayerCmp.Player = player;
            session.AddComponent<PingComponent>();
        }
        
        response.PlayerId = player.Id;
        
        Log.Info($"玩家 {player.Id} 登录成功!");
        await ETTask.CompletedTask;
    }
}