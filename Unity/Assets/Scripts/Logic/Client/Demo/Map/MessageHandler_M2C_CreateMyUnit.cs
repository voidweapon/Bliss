using ET;

namespace Bliss
{
    [MessageHandler(ELogicSceneType.Client)]
    public class MessageHandler_M2C_CreateMyUnit : MessageHandler<LogicScene,M2C_CreateMyUnit>
    {
        protected override async ETTask Run(LogicScene clientScene, M2C_CreateMyUnit message)
        {
            Log.Info($"进入地图 M2C_CreateMyUnit ");

            clientScene.GetComponent<ObjectWait>().Notify(new Wait_CreateMyUnit()
            {
                Message = message,
            }, true);
            
            await ETTask.CompletedTask;
        }
    }
}