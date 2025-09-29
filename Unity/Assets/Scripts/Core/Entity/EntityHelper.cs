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
        
        public static LogicScene Root(this Entity entity)
        {
            return entity.LogicScene.Root.Scene;
        }
        
        public static TimerComponent RootTimerComponent(this Entity entity)
        {
            return entity.Root().GetComponent<TimerComponent>();
        }
        
        public static CoroutineLockComponent RootCoroutineLockComponent(this Entity entity)
        {
            return entity.Root().GetComponent<CoroutineLockComponent>();
        }
    }
}