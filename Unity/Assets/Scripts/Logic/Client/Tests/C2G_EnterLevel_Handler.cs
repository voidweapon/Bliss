using ET;

namespace Bliss
{
    [SessionMessageHandler(ELogicSceneType.Client)]
    public class C2G_EnterLevel_Handler : SessionMessageHandler<C2G_EnterLevel, G2C_EnterLevel>
    {
        protected override async ETTask Run(Session session, C2G_EnterLevel message, G2C_EnterLevel response)
        {
            var playerMgr = session.Scene<LogicScene>().GetComponent<LevelPlayerMgrComponent>();
            var player = playerMgr.AddChild<Player>();
            player.AddComponent<PlayerNetworkComponent, Session>(session);
            await ETTask.CompletedTask;
        }
    }
}