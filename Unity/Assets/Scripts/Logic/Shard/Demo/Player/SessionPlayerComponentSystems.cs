using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(SessionPlayerComponent))]

    public static partial class SessionPlayerComponentSystems
    {
        private static void Awake(this SessionPlayerComponent self)
        {
            self.DestroyReason = 0;
        }
        
        private static void Destroy(this SessionPlayerComponent self)
        {
#if DOTNET
            if (self.DestroyReason == 0)
            {
                //断开链接
                self.Player.Offline();
            }
#endif
            self.Player = null;
        }
    }
}