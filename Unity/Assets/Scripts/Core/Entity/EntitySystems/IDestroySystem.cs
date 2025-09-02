using System;

namespace Bliss
{
    public interface IDestroy : IEntitySystemConstraint
    {
        
    }
    
    public interface IDestroySystem :ISystem
    {
        void Run(Entity entity);
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

        public void Run(Entity entity)
        {
            Destroy((TEntity)entity);
        }
        
        protected abstract void Destroy(TEntity entity);
    }
}