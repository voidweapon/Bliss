namespace Bliss
{
    /// <summary>
    /// 索引大世界下当前场景区块
    /// </summary>
    public class CurrentSceneComponent : Entity
    {
        
        private EntityRef<GameplayWorld> gameplayWorldRef;
        public GameplayWorld GameplayWorld
        {
            get
            {
                return (GameplayWorld)gameplayWorldRef;
            }
            set
            {
                gameplayWorldRef = value;
            }
        }
        
        private EntityRef<LogicScene> _currentLevelRef;
        public LogicScene CurrentScene
        {
            get
            {
                return (LogicScene)_currentLevelRef;
            }
            set
            {
                _currentLevelRef = value;
            }
        }
    }
}