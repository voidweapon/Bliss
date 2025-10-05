using System;

namespace Bliss
{
    /// <summary>
    /// 序列化之前触发
    /// </summary>
    public interface ISerialize : IEntitySystemConstraint
    {
        
    }
    
    public interface ISerializeSystem : ISystem
    {
        void Run(Entity entity);
    }

    public abstract class SerializeSystem<TEntity> : SystemObject, ISerializeSystem where TEntity : Entity, ISerialize
    {
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(ISerializeSystem);
        }

        public ESystemUpdateGroup GetUpdateGroup()
        {
            return ESystemUpdateGroup.None;
        }

        public void Run(Entity entity)
        {
            throw new NotImplementedException();
        }
        
        protected abstract void Serialize(TEntity self);
    }
}