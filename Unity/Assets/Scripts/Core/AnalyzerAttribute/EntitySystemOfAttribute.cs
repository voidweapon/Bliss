using System;

namespace Bliss.Core
{
    /// <summary>
    /// 标记Entity的System静态类 用于自动生成System函数
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EntitySystemOfAttribute : Attribute
    {
        public Type EntityType { get; private set; }

        public EntitySystemOfAttribute(Type entityType)
        {
            EntityType = entityType;
        }
    }
}