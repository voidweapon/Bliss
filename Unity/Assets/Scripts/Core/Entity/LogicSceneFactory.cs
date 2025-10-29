namespace Bliss
{
    public static class LogicSceneFactory
    {
        public static LogicScene CreateLogicScene(Entity parent, 
            long id,
            long instanceId,
            ELogicSceneType sceneType, 
            string name)
        {
            var scene = new LogicScene(parent.Root().Root, id, instanceId, sceneType, name);
            parent.AddChild(scene);
            return scene;
        }
    }
}