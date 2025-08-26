namespace Bliss
{
    public interface ILogicScene
    {
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

        public LogicScene()
        {
            
        }

        public LogicScene(Root root, int id, ELogicSceneType sceneType, string name)
        {
            Root = root;
            Id = id;
            SceneType = sceneType;
            Name = name; 
        }
    }
}