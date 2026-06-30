using Bliss.Timer;

namespace Bliss
{
    public static class EntityHelper
    {
        public static LogicScene Scene(this Entity entity)
        {
            return entity.LogicScene as LogicScene;
        }
        
        public static T Scene<T>(this Entity entity) where T: class, ILogicScene 
        {
            return entity.LogicScene as T;
        }
        
        public static LogicScene RootScene(this Entity entity)
        {
            return entity.LogicScene.Root.Scene;
        }
        
        public static Root Root(this Entity entity)
        {
            return entity.LogicScene.Root;
        }
        
        
        public static TimerComponent RootTimerComponent(this Entity entity)
        {
            return entity.RootScene().GetComponent<TimerComponent>();
        }
        
        public static CoroutineLockComponent RootCoroutineLockComponent(this Entity entity)
        {
            return entity.RootScene().GetComponent<CoroutineLockComponent>();
        }
        
        public static TComponent GetSceneComponent<TComponent>(this Entity self) where TComponent : Entity
        {
            return self.Scene<LogicScene>()?.GetComponent<TComponent>();
        }
    }
}