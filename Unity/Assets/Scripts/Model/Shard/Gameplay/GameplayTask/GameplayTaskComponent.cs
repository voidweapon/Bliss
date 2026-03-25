namespace Bliss.Gameplay.GameplayTasks
{
    public class GameplayTaskComponent : IGameplayTaskOwnerInterface
    {
        public GameplayTaskComponent GetGameplayTaskComponent(GameplayTask task)
        {
            throw new System.NotImplementedException();
        }

        public Entity GetGameplayTaskOwner(GameplayTask task)
        {
            throw new System.NotImplementedException();
        }

        public Entity GetGameplayTaskAvatar(GameplayTask task)
        {
            throw new System.NotImplementedException();
        }

        public byte GetGameplayTaskDefaultPriority(GameplayTask task)
        {
            throw new System.NotImplementedException();
        }

        public void OnGameplayTaskInitialized(GameplayTask task)
        {
            throw new System.NotImplementedException();
        }

        public void OnGameplayTaskActivated(GameplayTask task)
        {
            throw new System.NotImplementedException();
        }

        public void OnGameplayTaskDeactivated(GameplayTask task)
        {
            throw new System.NotImplementedException();
        }
    }
}