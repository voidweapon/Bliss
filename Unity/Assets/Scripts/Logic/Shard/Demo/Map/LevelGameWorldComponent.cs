namespace Bliss
{
    public class LevelGameWorldComponent : Entity, IEarlyUpdate, IUpdate, ILateUpdate
    {
        public GameplayWorld GameplayWorld;
    }

    public static class LevelGameWorldComponentHelper
    {
        public static GameplayWorld GetLevelGameplayWorld(this Entity entity)
        {
            if (entity.Scene() is GameplayWorld gameplayWorld) return gameplayWorld;
            var cmp = entity.GetSceneComponent<LevelGameWorldComponent>();
            return cmp?.GameplayWorld;
        }
    }
}