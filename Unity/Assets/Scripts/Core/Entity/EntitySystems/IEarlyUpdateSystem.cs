using System;

namespace Bliss
{
    public interface IEarlyUpdate : IEntitySystemConstraint
    {
        
    }
    
    public interface IEarlyUpdateSystem : IUpdateGroupSystem
    {
        
    }

    
    public abstract class EarlyUpdateSystem<TEntity> : SystemObject, IEarlyUpdateSystem where TEntity : Entity, IEarlyUpdate
    {
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(IEarlyUpdateSystem);
        }

        public ESystemUpdateGroup GetUpdateGroup()
        {
            return ESystemUpdateGroup.EarlyUpdate;
        }

        public void Run(Entity entity)
        {
            EarlyUpdate((TEntity)entity);
        }
        
        protected abstract void EarlyUpdate(TEntity entity);
    }
}