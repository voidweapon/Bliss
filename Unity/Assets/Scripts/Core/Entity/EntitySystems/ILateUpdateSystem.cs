using System;

namespace Bliss
{
    public interface ILateUpdate : IEntitySystemConstraint
    {
        
    }
    
    public interface ILateUpdateSystem : IUpdateGroupSystem
    {
    }
    
    public abstract class ALateUpdateSystem<TEntity> : SystemObject, ILateUpdateSystem where TEntity : Entity, ILateUpdate
    {
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(ILateUpdateSystem);
        }

        public ESystemUpdateGroup GetUpdateGroup()
        {
            return ESystemUpdateGroup.Update;
        }

        public void Run(Entity entity)
        {
            LateUpdate((TEntity)entity);
        }
        
        protected abstract void LateUpdate(TEntity entity);
    }
}