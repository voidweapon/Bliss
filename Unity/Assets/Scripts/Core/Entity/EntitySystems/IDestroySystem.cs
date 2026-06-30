using System;

namespace Bliss
{
    public interface IDestroy : IEntitySystemConstraint
    {
        
    }
    
    public interface IDestroySystem : IEventSystem
    {
    }
    
    public abstract class DestroySystem<TEntity> : SystemObject, IDestroySystem where TEntity : Entity, IDestroy
    {
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(IDestroySystem);
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
            Destroy((TEntity)entity);
        }
        
        protected abstract void Destroy(TEntity entity);
    }
}