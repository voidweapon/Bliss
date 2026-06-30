using System;

namespace Bliss
{
    /// <summary>
    /// 反序列化出来后，首次挂载到场景时触发
    /// </summary>
    public interface IDeserialize : IEntitySystemConstraint
    {
        
    }
    
    public interface IDeserializeSystem : IEventSystem
    {
    }

    public abstract class DeserializeSystem<TEntity> : SystemObject, IDeserializeSystem where TEntity : Entity, IDeserialize
    {
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(IDeserializeSystem);
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
            Deserialize((TEntity)entity);
        }
        
        protected abstract void Deserialize(TEntity self);
        
    }
    
}