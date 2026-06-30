namespace Bliss
{
    public class LevelGameWorldComponent : Entity
    {
        public LogicScene GameplayWorld;
    }

    public static class LevelGameWorldComponentHelper
    {
        public static LogicScene GetLevelGameplayWorld(this Entity entity)
        {
            if (entity.Scene().SceneType == ELogicSceneType.GameplayWorld) return entity.Scene();
            var cmp = entity.GetSceneComponent<LevelGameWorldComponent>();
            return cmp?.GameplayWorld;
        }
    }
}