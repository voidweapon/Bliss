using Bliss.ViewModel;
using ET;

namespace Bliss
{

    [Event(ELogicSceneType.GameplayWorld)]
    public class EventHandler_AfterCreateGameplayWorld : AEvent<LogicScene, Event_AfterCreateGameplayWorld>
    {
        protected override async ETTask Run(LogicScene gameplayWorld, Event_AfterCreateGameplayWorld eventInfo)
        {
            gameplayWorld.AddComponent<ResourcesLoaderComponent>();
            await ETTask.CompletedTask;
        }
    }
}