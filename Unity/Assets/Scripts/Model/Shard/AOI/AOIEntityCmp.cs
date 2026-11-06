using System.Collections.Generic;

namespace Bliss
{
    public class AOIEntityCmp : Entity, IAwake, IDestroy
    {
        public Dictionary<int, long> DicType2AoiIdentifier = new Dictionary<int, long>();
    }
}