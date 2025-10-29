using Bliss.ViewModel;
using ET;

namespace Bliss.ViewLogic.Tests
{
    [Event(ELogicSceneType.Client)]
    public class EventHandler_SyncViewTransform : AEvent<LogicScene, Event_SyncViewTransform>
    {
        protected override async ETTask Run(LogicScene mainScene, Event_SyncViewTransform eventInfo)
        {
            Entity entity = (Entity)eventInfo.Entity;
            if (entity == null)
            {
                return;
            }

            var viewCmp = entity.GetComponent<GameObjectViewComponent>();
            if (viewCmp == null || viewCmp.View == null)
            {
                return;
            }

            if (eventInfo.Flags.HasFlag(ETransformChangeFlag.Position))
            {
                if (eventInfo.IsLocalTransform)
                {
                    viewCmp.View.transform.localPosition = eventInfo.Position;   
                }
                else
                {
                    viewCmp.View.transform.position = eventInfo.Position;   
                }
            }
            
            if (eventInfo.Flags.HasFlag(ETransformChangeFlag.Rotation))
            {
                if (eventInfo.IsLocalTransform)
                {
                    viewCmp.View.transform.rotation = eventInfo.Rotation;   
                }
                else
                {
                    viewCmp.View.transform.localRotation = eventInfo.Rotation;   
                }
            }
            
            if (eventInfo.Flags.HasFlag(ETransformChangeFlag.Scale))
            {
                viewCmp.View.transform.localScale = eventInfo.Scale;   
            }
            
            await ETTask.CompletedTask;
        }
    }
}