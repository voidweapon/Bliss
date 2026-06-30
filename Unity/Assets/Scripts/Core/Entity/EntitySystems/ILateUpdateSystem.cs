using System;
using Bliss.Core;

namespace Bliss
{
    public interface ILateUpdate : IEntitySystemConstraint
    {
        
    }
    
    public interface ILateUpdateSystem : IUpdateGroupSystem
    {
    }
    
    
    public abstract class LateUpdateSystem<TEntity> : SystemObject, ILateUpdateSystem where TEntity : Entity, ILateUpdate
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
            return ESystemUpdateGroup.LateUpdate;
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
            LateUpdate((TEntity)entity);
        }
        
        protected abstract void LateUpdate(TEntity entity);
    }
}