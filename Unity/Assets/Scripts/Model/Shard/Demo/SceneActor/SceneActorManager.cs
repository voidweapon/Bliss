using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// 挂载所有的SceneActor
    /// </summary>
    public class SceneActorManager : Entity
    {
        public Dictionary<long, EntityRef<SceneActor>> DicEntityId2SceneActor = new Dictionary<long, EntityRef<SceneActor>>();
    }
}