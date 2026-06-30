using ET;

namespace Bliss
{
    [MessageHandler(ELogicSceneType.Client)]
    public class MessageHandler_M2C_CreateSceneActors : MessageHandler<LogicScene,M2C_CreateSceneActors>
    {
        protected override async ETTask Run(LogicScene clientScene, M2C_CreateSceneActors message)
        {
            Log.Info($" M2C_CreateSceneActors ");
            
            var currentSceneComponent = clientScene.GetComponent<CurrentSceneComponent>();
            var levelScene = currentSceneComponent.CurrentScene;

            if (levelScene == null)
            {
                var waiter = await clientScene.GetComponent<ObjectWait>().Wait<Wait_CreateLevelScene>();
                if (waiter.Error != WaitTypeError.Success)
                {
                    return;
                }
            }
            var gameplayWorld = currentSceneComponent.GameplayWorld;

            LogicSceneFactory.CreateSceneActor(gameplayWorld, message.SceneActors, ENetRole.SimulatedProxy, ENetRole.Authority);
        }
    }
}