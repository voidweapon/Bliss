using System;

namespace Bliss
{
    /// <summary>
    /// 挂载到场景时触发
    /// 首次挂载到场景和更改场景挂载都会触发
    /// </summary>
    public interface IMountToScene : IEntitySystemConstraint
    {
        
    }
    
    public interface IMountToSceneSystem : ISystem
    {
        void Run(Entity entity);
    }
    public abstract class MountToSceneSystem<TEntity> : SystemObject, IMountToSceneSystem where TEntity : Entity,IMountToScene
    {
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public Type GetSystemCategoryType()
        {
            return typeof(IMountToSceneSystem);
        }

        public ESystemUpdateGroup GetUpdateGroup()
        {
            return ESystemUpdateGroup.None;
        }

        public void Run(Entity entity)
        {
            MountToScene((TEntity)entity);
        }
        
        protected abstract void MountToScene(TEntity self);
    }
}