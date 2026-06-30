using System;
using Bliss.Core;

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
            EarlyUpdate((TEntity)entity);
        }
        
        protected abstract void EarlyUpdate(TEntity entity);
    }
}