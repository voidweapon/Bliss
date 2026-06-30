using Bliss.CharacterControllers;
using ET;

namespace Bliss
{
    [Event(ELogicSceneType.Client)]
    public class EventHandler_EndChangeLevelScene  : AEvent<LogicScene, Event_EndChangeLevelScene>
    {
        protected override async ETTask Run(LogicScene scene, Event_EndChangeLevelScene eventInfo)
        {
            var uiComponent = scene.GetComponent<UIComponent>();
            uiComponent.Remove(UIType.Loading);
            await ETTask.CompletedTask;
        }
    }
}