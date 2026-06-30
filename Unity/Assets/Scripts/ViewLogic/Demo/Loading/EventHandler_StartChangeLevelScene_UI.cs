using ET;

namespace Bliss
{
    [Event(ELogicSceneType.Client)]
    public class EventHander_StartChangeLevelScene_UI : AEvent<LogicScene, Event_StartChangeLevelScene>
    {
        protected override async ETTask Run(LogicScene scene, Event_StartChangeLevelScene eventInfo)
        {
            await ETTask.CompletedTask;
        }
    }
}