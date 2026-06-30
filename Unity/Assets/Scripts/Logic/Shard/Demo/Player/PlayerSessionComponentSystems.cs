using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(PlayerSessionComponent))]
    public static partial class PlayerSessionComponentSystems
    {
        private static void Destroy(this PlayerSessionComponent self)
        {
            self.Session = null;
        }
    }
}