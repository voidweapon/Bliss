using System;
using System.IO;
using Bliss.Base.Collections;

namespace Bliss
{
    public static partial class LogicSceneFactoryCore
    {
        public static LogicScene CreateLogicScene(Entity parent, 
            long id,
            long instanceId,
            ELogicSceneType sceneType, 
            string name)
        {
            if ((sceneType & ELogicSceneType.Level) != 0)
            {
                throw new InvalidOperationException("Level scenes must be created through LevelSceneFactory.");
            }

            var scene = new LogicScene(parent.RootScene().Root, id, instanceId, sceneType, name);
            parent.AddChild(scene);
            return scene;
        }
        
    }
}