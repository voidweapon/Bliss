namespace Bliss
{
    /// <summary>
    /// 索引大世界下当前场景区块
    /// </summary>
    public class CurrentSceneComponent : Entity
    {
        
        private EntityRef<LogicScene> _gameplayWorldRef;
        public LogicScene GameplayWorld
        {
            get
            {
                return (LogicScene)_gameplayWorldRef;
            }
            set
            {
                _gameplayWorldRef = value;
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