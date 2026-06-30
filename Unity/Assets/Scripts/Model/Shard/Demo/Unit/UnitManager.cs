using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// 存放所有的Unity
    /// </summary>
    public class UnitManager : Entity
    {
        public long ObserveUnitId;
        
        public Dictionary<long, EntityRef<Unit>> DicEntityId2Unit = new Dictionary<long, EntityRef<Unit>>();
    }
}