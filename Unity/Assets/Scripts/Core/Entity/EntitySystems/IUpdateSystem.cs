using System;

namespace Bliss
{
    public interface IUpdate : IEntitySystemConstraint
    {
        
    }
    
    public interface IUpdateSystem : IUpdateGroupSystem
    {
        
    }

    
    public abstract class UpdateSystem<TEntity> : SystemObject, IUpdateSystem where TEntity : Entity, IUpdate
    {
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(IUpdateSystem);
        }

        public ESystemUpdateGroup GetUpdateGroup()
        {
            return ESystemUpdateGroup.Update;
        }

        public void Run(Entity entity)
        {
            Update((TEntity)entity);
        }
        
        protected abstract void Update(TEntity entity);
    }
}