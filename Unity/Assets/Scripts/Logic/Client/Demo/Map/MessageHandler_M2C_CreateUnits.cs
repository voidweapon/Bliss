using ET;

namespace Bliss
{
    [MessageHandler(ELogicSceneType.Client)]
    public class MessageHandler_M2C_CreateUnits : MessageHandler<LogicScene,M2C_CreateUnits>
    {
        protected override async ETTask Run(LogicScene clientScene, M2C_CreateUnits message)
        {
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

            foreach (var dtoUnitInfo in message.Units)
            {
                //从服务过来的NetRole,在本地需要对调一下
                (dtoUnitInfo.LocalRole, dtoUnitInfo.RemoteRole) = (dtoUnitInfo.RemoteRole, dtoUnitInfo.LocalRole);
                dtoUnitInfo.CreateUnit(gameplayWorld);
            }
        }
    }
}