namespace Bliss
{
    /// <summary>
    /// Updates GameplayWorld entities through the shared entity update scheduler.
    /// </summary>
    public class GameplayWorldUpdater : BObject
    {
        private readonly EntityUpdateScheduler updateScheduler = new EntityUpdateScheduler();
        private readonly GameplayWorld gameplayWorld;

        /// <summary>Creates a standalone updater that is not associated with a GameplayWorld.</summary>
        public GameplayWorldUpdater()
        {
        }

        internal GameplayWorldUpdater(GameplayWorld gameplayWorld)
        {
            this.gameplayWorld = gameplayWorld;
        }

        /// <summary>
        /// Registers an entity for updates. Call once for each entity.
        /// </summary>
        public void Add(Entity entity)
        {
            updateScheduler.Register(
                entity,
                EntitySystemManager.Instance.GetEntityUpdateSystems(entity.GetType()));
        }

        /// <summary>
        /// Starts the frame and runs the EarlyUpdate group range unless its World is faulted.
        /// </summary>
        public void EarlyUpdate()
        {
            if (this.gameplayWorld != null && this.gameplayWorld.IsFaulted)
            {
                return;
            }

            updateScheduler.BeginFrame();
            updateScheduler.Update(
                (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_EARLY_UPDATE,
                ESystemUpdateGroup.EarlyUpdate);
        }

        /// <summary>
        /// Runs the Update group range for the current frame unless its World is faulted.
        /// </summary>
        public void Update()
        {
            if (this.gameplayWorld != null && this.gameplayWorld.IsFaulted)
            {
                return;
            }

            updateScheduler.Update(
                (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_UPDATE,
                ESystemUpdateGroup.Update);
        }

        /// <summary>
        /// Runs the LateUpdate group range for the current frame unless its World is faulted.
        /// </summary>
        public void LateUpdate()
        {
            if (this.gameplayWorld != null && this.gameplayWorld.IsFaulted)
            {
                return;
            }

            updateScheduler.Update(
                (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_LATE_UPDATE,
                ESystemUpdateGroup.LateUpdate);
        }
    }
}
