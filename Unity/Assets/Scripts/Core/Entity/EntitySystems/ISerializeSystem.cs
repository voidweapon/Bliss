using System;

namespace Bliss
{
    /// <summary>
    /// 序列化之前触发
    /// </summary>
    public interface ISerialize : IEntitySystemConstraint
    {
        
    }
    
    public interface ISerializeSystem : IEventSystem
    {
    }

    public abstract class SerializeSystem<TEntity> : SystemObject, ISerializeSystem where TEntity : Entity, ISerialize
    {
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(ISerializeSystem);
        }

        public ESystemUpdateGroup GetUpdateGroup()
        {
            return ESystemUpdateGroup.None;
        }
        
        public virtual int GetPriority()
        {
            return 0;
        }
        public int CompareTo(ISystem other)
        {
            return GetPriority() - other.GetPriority();
        }

        public void Run(Entity entity)
        {
            throw new NotImplementedException();
        }
        
        protected abstract void Serialize(TEntity self);
    }
}