namespace Bliss
{
    public class GameplayWorld : LogicScene
    {
        public GameplayWorldUpdater Updater { get; } = new GameplayWorldUpdater();

        internal GameplayWorld(Root root, long id, long instanceId, string name)
            : base(root, id, instanceId, ELogicSceneType.GameplayWorld, name)
        {
        }
        
        public void Register(Entity entity)
        {
            Updater.Add(entity);
        }

        public void Unregister(Entity entity)
        {
        }

        public void EarlyUpdate()
        {
            Updater.EarlyUpdate();
        }

        public void Update()
        {
            Updater.Update();
        }

        public void LateUpdate()
        {
            Updater.LateUpdate();
        }
    }
}