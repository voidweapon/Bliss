using System;

namespace Bliss.Core
{
    /// <summary>
    /// 用于控制EntitySystem的执行顺序
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SystemPriorityAttribute : Attribute
    {
        public Type SystemType  { get; private set; }
        public int Priority { get; private set; }

        public SystemPriorityAttribute(Type type, int priority)
        {
            SystemType = type;
            Priority = priority;
        }
    }
}