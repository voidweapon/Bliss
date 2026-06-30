namespace Bliss
{
    public  static partial class SceneActorManagerSystems
    {
        public static void Add(this SceneActorManager self, SceneActor sceneActor)
        {
            self.DicEntityId2SceneActor.Add(sceneActor.Id, sceneActor);
        }

        public static void Remove(this SceneActorManager self, long id)
        {
            self.DicEntityId2SceneActor.Remove(id);
        }
        
        
    }
}