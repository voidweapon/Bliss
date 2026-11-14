using System;

namespace Bliss
{
    public interface ILogicScene
    {
        Root Root { get; set; }
        ELogicSceneType SceneType { get; set; } 
    }

    /// <summary>
    /// Allows a scene to defer entity update registration until its initialization commits.
    /// </summary>
    public interface IEntityRegistrationDeferral
    {
        /// <summary>
        /// Defers registration of an entity when its scene is still initializing.
        /// </summary>
        /// <param name="entity">The entity being attached to the scene.</param>
        /// <param name="register">The Root update registration operation to defer.</param>
        /// <param name="unregister">The Root update unregistration operation used for rollback.</param>
        /// <returns>True when registration is deferred.</returns>
        bool DeferRegistration(
            Entity entity,
            Action<Entity> register,
            Action<Entity> unregister);
    }
    
    public class LogicScene : Entity, ILogicScene
    {
        public Root Root { get; set; }
        public ELogicSceneType SceneType { get; set; }
        /// <summary>
        /// 逻辑场景名称
        /// </summary>
        public string Name;

        protected LogicScene()
        {
            
        }

        protected internal LogicScene(Root root, long id,  long instanceId, ELogicSceneType sceneType, string name)
        {
            Root = root;
            Id = id;
            InstanceId = instanceId;
            SceneType = sceneType;
            Name = name;
            _logicScene = this;
        }
    }
}