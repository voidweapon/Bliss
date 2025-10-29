using ET;

namespace Bliss
{
    [MessageHandler(ELogicSceneType.Client)]
    public class MessageHandler_PropertySync_LongMessage : MessageHandler<SceneActor, PropertySync_Long>
    {
        protected override async ETTask Run(SceneActor entity, PropertySync_Long message)
        {
            var motionCmp =  entity.GetComponent<PeriodicMotionComponent>();
            if (motionCmp == null)
            {
                return;
            }
            
            motionCmp.ReceivePropertySync(message);
            
            await ETTask.CompletedTask;
        }
    }
}