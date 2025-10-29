using ET;

namespace Bliss;

[Event(ELogicSceneType.Main)]
public class EventHandler_OnAcceiptConnection_Outer : AEvent<ILogicScene, Event_OnAcceiptConnection>
{
    protected override async ETTask Run(ILogicScene scene, Event_OnAcceiptConnection eventInfo)
    {
        eventInfo.Session.AddComponent<PingComponent>();
        await ETTask.CompletedTask;
    }
}