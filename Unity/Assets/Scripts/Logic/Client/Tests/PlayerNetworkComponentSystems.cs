using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(PlayerNetworkComponent))]
    public static partial class PlayerNetworkComponentSystems
    {
        public static void Awake(this PlayerNetworkComponent self, Session param1)
        {
            self.Session =  param1;
        }
        
        public static void Destroy(this PlayerNetworkComponent self)
        {
            if (self.Session != null)
            {
                self.Session.Dispose();
            }

            self.Session = null;
        }
    }
}