using System;
using ET;

namespace Bliss
{
    [MessageHandler(ELogicSceneType.Client)]
    public class MessageHandler_M2C_CreateBullets : MessageHandler<LogicScene,M2C_CreateBullets>
    {
        protected override async ETTask Run(LogicScene clientScene, M2C_CreateBullets message)
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

            foreach (var dtoBulletInfo in message.Bullets)
            {
                //从服务过来的NetRole,在本地需要对调一下
                (dtoBulletInfo.LocalRole, dtoBulletInfo.RemoteRole) = (dtoBulletInfo.RemoteRole, dtoBulletInfo.LocalRole);
                dtoBulletInfo.CreateBullet(gameplayWorld);
            }
        }
    }
}