using System;

namespace Bliss
{
    /// <summary>
    /// 表示任务被系统取消
    /// </summary>
    public class TaskCanceledException : Exception
    {
        public TaskCanceledException()
        {
            
        }
        
        public TaskCanceledException(string msg) : base(msg)
        {
            
        }
    }
    
    /// <summary>
    /// 表示任务被用户取消
    /// </summary>
    public class TaskAbortedException : Exception
    {

    }
    

    
}