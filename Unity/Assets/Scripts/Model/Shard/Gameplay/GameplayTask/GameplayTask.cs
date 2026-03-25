using System;

namespace Bliss.Gameplay.GameplayTasks
{
    public enum EGameplayTaskState
    {
        Uninitialized,
        AwaitingActivation,
        Paused,
        Active,
        Finished
    }
    
    public enum ETaskResourceOverlapPolicy : byte
    {
        /// <summary>
        /// Pause overlapping same-priority tasks.
        /// </summary>
        StartOnTop,
        
        /// <summary>
        /// Wait for other same-priority tasks to finish. 
        /// </summary>
        StartAtEnd,
        
        /// <summary>
        ///  Request to cancel same or lower priority tasks, if tasks not ended pause overlapping same-priority tasks.
        /// </summary>
        RequestCancelAndStartOnTop,
        
        /// <summary>
        /// Request to cancel same or lower priority tasks, wait for any remaining overlapping same-priority tasks to finish.
        /// </summary>
        RequestCancelAndStartAtEnd
    }
    public class GameplayTask
    {
        protected string InstanceName;

        /// <summary>
        /// Called when the task is asked to confirm from an outside node. What this means depends on the individual task. By default, this does nothing other than ending if bEndTask is true.
        /// </summary>
        /// <param name="bEndTask"></param>
        public virtual void ExternalConfirm(bool bEndTask)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when the task is asked to cancel from an outside node. What this means depends on the individual task. By default, this does nothing other than ending the task.
        /// </summary>
        public virtual void ExternalCancel()
        {
            throw new NotImplementedException();
        }
        
        public string GetInstanceName()
        {
            return InstanceName;
        }

        /// <summary>
        /// Called explicitly to end the task (usually by the task itself). Calls OnDestroy. 
        ///	@NOTE: you need to call EndTask before sending out any "on completed" delegates. 
        ///	If you don't the task will still be in an "active" state while the event receivers may
        /// assume it's already "finished" 
        /// </summary>
        public void EndTask()
        {
            throw new NotImplementedException();
        }

        public virtual bool IsWaitingOnRemotePlayerdata()
        {
            return false;
        }

        public virtual bool IsWaitingOnAvatar()
        {
            return false;
        }
    }
}