using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// AOI 管理组件。
    /// 维护注册条目、内部句柄、Tag/网格索引、双向可见关系与脏集合。
    /// </summary>
    public class AoiManagerComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// AOI 主表：identifier -> AOI 条目。
        /// </summary>
        public Dictionary<long, AoiEntry> Entries = new();

        /// <summary>
        /// Tag 索引：tag -> identifier 集合。
        /// </summary>
        public Dictionary<int, HashSet<long>> TagToEntityIds = new();

        /// <summary>
        /// 空间网格索引：cellKey -> identifier 集合。
        /// </summary>
        public Dictionary<AoiCellKey, HashSet<long>> CellToEntityIds = new();

        /// <summary>
        /// 反向网格覆盖索引：identifier -> coveredCellKey 集合。
        /// </summary>
        public Dictionary<long, HashSet<AoiCellKey>> EntityToCellKeys = new();

        /// <summary>
        /// 正向可见关系：observerIdentifier -> targetIdentifier 集合。
        /// </summary>
        public Dictionary<long, HashSet<long>> VisibleTargetsByObserver = new();

        /// <summary>
        /// 反向可见关系：targetIdentifier -> observerIdentifier 集合。
        /// </summary>
        public Dictionary<long, HashSet<long>> VisibleObserversByTarget = new();

        /// <summary>
        /// 待重算可见关系的 AOI 句柄集合。
        /// </summary>
        public HashSet<long> DirtyEntityIds = new();

        /// <summary>
        /// 内部句柄生成器。
        /// </summary>
        public long IdentifierGenerator;

        /// <summary>
        /// 进入可见回调：observerId, targetId, targetTag。
        /// </summary>
        public Action<long, long, int> OnEnterVisible;

        /// <summary>
        /// 离开可见回调：observerId, targetId, targetTag。
        /// </summary>
        public Action<long, long, int> OnLeaveVisible;
    }
}
