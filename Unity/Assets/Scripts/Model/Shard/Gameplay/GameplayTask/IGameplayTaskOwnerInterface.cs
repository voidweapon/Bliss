namespace Bliss.Gameplay.GameplayTasks
{
    public interface IGameplayTaskOwnerInterface
    {
        /// <summary>
        /// Finds tasks component for given GameplayTask, Task.GetGameplayTasksComponent() may not be initialized at this point!
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        GameplayTaskComponent GetGameplayTaskComponent(GameplayTask task);
        
        /// <summary>
        /// Get owner of a task or default one when task is null
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        Entity GetGameplayTaskOwner(GameplayTask task);
        
        /// <summary>
        /// Get "body" of task's owner / default, having location in world (e.g. Owner = AIController, Avatar = Pawn)
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        Entity GetGameplayTaskAvatar(GameplayTask task);
        
        /// <summary>
        /// Get default priority for running a task 
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        byte GetGameplayTaskDefaultPriority(GameplayTask task);
        
        /// <summary>
        /// Notify called after GameplayTask finishes initialization (not active yet)
        /// </summary>
        /// <param name="task"></param>
        void OnGameplayTaskInitialized(GameplayTask task);
        
        /// <summary>
        /// Notify called after GameplayTask changes state to Active (initial activation or resuming)
        /// </summary>
        /// <param name="task"></param>
        void OnGameplayTaskActivated(GameplayTask task);
        
        /// <summary>
        /// Notify called after GameplayTask changes state from Active (finishing or pausing) 
        /// </summary>
        /// <param name="task"></param>
        void OnGameplayTaskDeactivated(GameplayTask task);
    }
}