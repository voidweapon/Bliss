using ET;

namespace Bliss
{
    [MessageHandler(ELogicSceneType.Client)]
    public class MessageHabdler_TransformSimpleSyncMessage : MessageHandler<SceneActor, TransformSimpleSyncMessage>
    {
        protected override async ETTask Run(SceneActor clientScene, TransformSimpleSyncMessage message)
        {
            var motionCmp =  clientScene.GetComponent<PeriodicMotionComponent>();
            if (motionCmp == null)
            {
                return;
            }
            
            motionCmp.ReceiveTransformSync(message);
            
            await ETTask.CompletedTask;
        }
    }
}