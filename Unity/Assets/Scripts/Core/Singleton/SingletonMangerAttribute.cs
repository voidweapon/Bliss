using System;
using Bliss.Core;

namespace Bliss
{
    /// <summary>
    /// 标记这个Manager自动添加到World
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SingletonMangerAttribute : BaseAttribute
    {
        public int Priority { get; }

        public SingletonMangerAttribute()
        {
            Priority = 0;
        }
        
        public SingletonMangerAttribute(int priority)
        {
            Priority = priority;
        }
    }
}