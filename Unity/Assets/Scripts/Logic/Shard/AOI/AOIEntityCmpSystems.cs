using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(AOIEntityCmp))]
    public static partial class AOIEntityCmpSystems
    {
        private static void Awake(this AOIEntityCmp self)
        {
            self.DicType2AoiIdentifier.Clear();
        }
        private static void Destroy(this AOIEntityCmp self)
        {
            var aoiManagerComponent = self.GetLevelGameplayWorld()?.GetComponent<AoiManagerComponent>();
            foreach (var kv in self.DicType2AoiIdentifier)
            {
                var aoiIdentifier = kv.Value;
                aoiManagerComponent?.Unregister(aoiIdentifier);
            }
            self.DicType2AoiIdentifier?.Clear();
        }

        public static void Add(this AOIEntityCmp self, int tag, long identifier)
        {
            self.DicType2AoiIdentifier.Add(tag, identifier);
        }

        public static bool GetId(this AOIEntityCmp self, int tag, out long identifier)
        {
            return self.DicType2AoiIdentifier.TryGetValue(tag, out identifier);
        }
    }

}