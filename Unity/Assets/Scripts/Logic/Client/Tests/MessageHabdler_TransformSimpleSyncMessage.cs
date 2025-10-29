using ET;

namespace Bliss
{
    [MessageHandler(ELogicSceneType.Client)]
    public class MessageHabdler_TransformSimpleSyncMessage : MessageHandler<SceneActor, TransformSimpleSyncMessage>
    {
        protected override async ETTask Run(SceneActor entity, TransformSimpleSyncMessage message)
        {
            var motionCmp =  entity.GetComponent<PeriodicMotionComponent>();
            if (motionCmp == null)
            {
                return;
            }
            
            motionCmp.ReceiveTransformSync(message);
            
            await ETTask.CompletedTask;
        }
    }
}