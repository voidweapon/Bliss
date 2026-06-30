using Bliss.Id;
using ET;

namespace Bliss
{
    /// <summary>
    /// 游戏场景工厂
    /// </summary>
    public static class LevelSceneFactory
    {
        public static async ETTask<LogicScene> Create(Entity parent, LogicScene gameplayWorld, int configId, long id, string name)
        {
            LogicScene levelScene = LogicSceneFactoryCore.CreateLogicScene(parent, id, IdGenerator.Instance.GenerateInstanceId(), ELogicSceneType.Level, name);
            var levelGameWorldComponent = levelScene.AddComponent<LevelGameWorldComponent>();
            levelGameWorldComponent.GameplayWorld = gameplayWorld;
            
            //加载客户端/服务器各自的组件
            await EventSystem.Instance.PushAsync(levelScene, new Event_AfterCreateLevelScene()
            {
                LevelConfigId = configId,
            });
            
            //
            
            return levelScene;
        }
    }
}