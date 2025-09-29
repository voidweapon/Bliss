using System.Collections.Generic;

namespace Bliss.Core.Network
{
    public class CombinedIpNetConnection : IpNetConnection
    {
        private List<IpNetConnection> _Connections;
    }
}