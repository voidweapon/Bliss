using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(Player))]
    public static partial class PlayerSystems
    {
        private static void Awake(this Player self, string param1)
        {
            self.Account = param1;
        }
    }
}