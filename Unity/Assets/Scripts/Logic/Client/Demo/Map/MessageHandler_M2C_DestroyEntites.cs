using System;
using ET;

namespace Bliss
{
    [MessageHandler(ELogicSceneType.Client)]
    public class MessageHandler_M2C_DestroyEntites : MessageHandler<LogicScene,M2C_DestroyEntities>
    {
        protected override async ETTask Run(LogicScene clientScene, M2C_DestroyEntities message)
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
            
            var networkEntityManager = clientScene.RootScene().GetComponent<NetworkEntityManagerComponent>();
            if (networkEntityManager == null)
            {
                Log.Error($"not found {nameof(NetworkEntityManagerComponent)}, scene: {clientScene.Name}");
                return;
            }

            foreach (var entityId in message.EntityIds)
            {
               var entityRef = networkEntityManager.GetEntity(entityId);
               var entity = (Entity)entityRef;
               if (entity == null)
               {
                   continue;
               }
               entity.Dispose();
            }
            
            await ETTask.CompletedTask;
        }
    }
}