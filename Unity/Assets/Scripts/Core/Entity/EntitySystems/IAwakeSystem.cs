using System;

namespace Bliss
{
    public interface IAwake : IEntitySystemConstraint
    {
        
    }
    
    public interface IAwake<TParam> : IEntitySystemConstraint
    {
        
    }
    
    
    /// <summary>
    /// pair <see cref="IAwake"/>
    /// </summary>
    /// <typeparam name="TParam"></typeparam>
    public interface IAwakeSystem : ISystem
    {
        void Run(Entity entity);
    }

    /// <summary>
    /// pair <see cref="IAwake{TParam}"/>
    /// </summary>
    /// <typeparam name="TParam"></typeparam>
    public interface IAwakeSystem<TParam> : ISystem
    {
        void Run(Entity entity, TParam param);
        
    }

    public abstract class AwakeSystem<TEntity> : SystemObject, IAwakeSystem where TEntity : Entity,IAwake
    {
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(IAwakeSystem);
        }

        public ESystemUpdateGroup GetUpdateGroup()
        {
           return ESystemUpdateGroup.None;
        }

        public virtual int GetPriority()
        {
            return 0;
        }

        public void Run(Entity entity)
        {
            Awake((TEntity)entity);
        }
        
        protected abstract void Awake(TEntity self);

        public int CompareTo(ISystem other)
        {
            return GetPriority() - other.GetPriority();
        }
    }
    
    public abstract class AwakeSystem<TEntity, TParamA> : SystemObject, IAwakeSystem<TParamA> where TEntity : Entity,IAwake<TParamA>
    {

        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(IAwakeSystem<TParamA>);
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
        
        public void Run(Entity entity, TParamA param)
        {
            Awake((TEntity)entity, param);
        }
        
        protected abstract void Awake(TEntity self, TParamA param);
    }
}