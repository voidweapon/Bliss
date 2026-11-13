namespace Bliss
{
    public class GameplayWorld : LogicScene
    {
        public GameplayWorldUpdater Updater { get; } = new GameplayWorldUpdater();

        /// <summary>
        /// Gets the timer service owned by this world.
        /// </summary>
        public GameplayWorldTimer Timer { get; }

        internal GameplayWorld(Root root, long id, long instanceId, string name)
            : base(root, id, instanceId, ELogicSceneType.GameplayWorld, name)
        {
            this.Timer = new GameplayWorldTimer(this);
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
            this.Timer.update();
            Updater.Update();
        }

        public void LateUpdate()
        {
            Updater.LateUpdate();
        }

        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.Timer.dispose();
            base.Dispose();
        }
    }
}