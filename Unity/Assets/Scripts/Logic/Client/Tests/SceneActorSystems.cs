using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(SceneActor))]
    public static partial class SceneActorSystems
    {
        public static void Awake(this SceneActor self)
        {
            //self.AddComponent<TransformComponent>();
        }
    }
}