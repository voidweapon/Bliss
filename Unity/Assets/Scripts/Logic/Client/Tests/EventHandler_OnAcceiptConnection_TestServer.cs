using Bliss.Core.EventSystem;
using ET;

namespace Bliss.Logic.Client
{
    [Event(ELogicSceneType.Client)]
    public class EventHandler_OnAcceiptConnection_TestServer : AEvent<ILogicScene, Event_OnAcceiptConnection>
    {
        protected override async ETTask Run(ILogicScene clientScene, Event_OnAcceiptConnection eventInfo)
        {
            if (clientScene is not LogicScene logicScene)
            {
                return;
            }

            if (logicScene.Name != "Server")
            {
                return;
            }
            
            eventInfo.Session.AddComponent<PingComponent>();
            await ETTask.CompletedTask;
        }
    }
}