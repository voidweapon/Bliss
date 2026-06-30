using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// 用于存放网络对象的容器
    /// </summary>
    public class LevelNetworkObjectCollectionComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, Entity> NetworkObjects = new Dictionary<long, Entity>();
    }
}