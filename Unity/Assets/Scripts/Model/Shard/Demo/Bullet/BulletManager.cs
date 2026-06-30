using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// 存放所有的Bullet
    /// </summary>
    public class BulletManager : Entity
    {
        public Dictionary<long, EntityRef<Bullet>> DicEntityId2Bullet = new Dictionary<long, EntityRef<Bullet>>();
    }
}