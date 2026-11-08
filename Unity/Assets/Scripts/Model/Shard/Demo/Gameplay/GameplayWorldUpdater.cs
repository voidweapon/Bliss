namespace Bliss
{
    /// <summary>
    /// Updates GameplayWorld entities through the shared entity update scheduler.
    /// </summary>
    public class GameplayWorldUpdater : BObject
    {
        private readonly EntityUpdateScheduler updateScheduler = new EntityUpdateScheduler();

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
        /// Starts the frame and runs the EarlyUpdate group range.
        /// </summary>
        public void EarlyUpdate()
        {
            updateScheduler.BeginFrame();
            updateScheduler.Update(
                (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_EARLY_UPDATE,
                ESystemUpdateGroup.EarlyUpdate);
        }

        /// <summary>
        /// Runs the Update group range for the current frame.
        /// </summary>
        public void Update()
        {
            updateScheduler.Update(
                (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_UPDATE,
                ESystemUpdateGroup.Update);
        }

        /// <summary>
        /// Runs the LateUpdate group range for the current frame.
        /// </summary>
        public void LateUpdate()
        {
            updateScheduler.Update(
                (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_LATE_UPDATE,
                ESystemUpdateGroup.LateUpdate);
        }
    }
}
