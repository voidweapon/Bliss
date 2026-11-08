using Bliss.CharacterMovement;
using Bliss.Id;
using ET;

namespace Bliss
{
    [MessageHandler(ELogicSceneType.Client)]
    public class MessageHandler_M2C_ChangeMap : MessageHandler<LogicScene, M2C_ChangeMap>
    {
        protected override async ETTask Run(LogicScene clientScene, M2C_ChangeMap message)
        {
            Log.Info($"进入地图 M2C_ChangeMap ");

            GameplayWorld gameplayWorld = clientScene.GetComponent<CurrentSceneComponent>()?.GameplayWorld;
            if (message.IsLogin)
            {
                gameplayWorld = await MapHelper.CreateGameplayWorld(clientScene);
            }
            
            await MapHelper.ChangeMap(clientScene, gameplayWorld, message.LevelConfigId, message.LevelId);
        }
    }
}