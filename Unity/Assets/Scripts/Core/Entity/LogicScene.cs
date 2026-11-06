namespace Bliss
{
    public interface ILogicScene
    {
        Root Root { get; set; }
        ELogicSceneType SceneType { get; set; } 
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

        internal LogicScene(Root root, long id,  long instanceId, ELogicSceneType sceneType, string name)
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