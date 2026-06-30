using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(ClientSessionComponent))]
    public static partial class ClientSessionComponentSystems
    {
        private static void Awake(this ClientSessionComponent self, Session param1)
        {
            self.Session = param1;
        }
        
        private static void Destroy(this ClientSessionComponent self)
        {
            var session = (Session)self.Session;
            session?.Dispose();
            self.Session = default;
        }
    }
}