using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// 管理Player的同步事宜
    /// </summary>
    public class ReplicationGraph : Entity, IAwake, IDestroy
    {
        public HashSet<EntityRef<RemoteGhost>> RemoteGhosts = new HashSet<EntityRef<RemoteGhost>>();
    }
}