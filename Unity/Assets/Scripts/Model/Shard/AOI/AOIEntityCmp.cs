using System.Collections.Generic;

namespace Bliss
{
    /// <summary>Tracks the owning gameplay entity's registered AOI identifiers.</summary>
    public class AOIEntityCmp : GamePlayEntity, IAwake, IDestroy
    {
        public Dictionary<int, long> DicType2AoiIdentifier = new Dictionary<int, long>();
    }
}