using System;
using Bliss.Core;

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
        public virtual int Priority { get; protected set; } = 0;
        
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(IUpdateSystem);
        }

        public virtual ESystemUpdateGroup GetUpdateGroup()
        {
            return ESystemUpdateGroup.Update;
        }
        
        public virtual int GetPriority()
        {
            return 0;
        }
        public int CompareTo(ISystem other)
        {
            return GetPriority().CompareTo(other.GetPriority());
        }

        public void Run(Entity entity)
        {
            Update((TEntity)entity);
        }
        
        protected abstract void Update(TEntity entity);
    }
}