using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Bliss
{
    public static class NetworkHelper
    {
        
        public static IPEndPoint ToIPEndPoint(string host, int port)
        {
            return new IPEndPoint(IPAddress.Parse(host), port);
        }
        
        
        public static IPEndPoint ToIPEndPoint(string address)
        {
            int index = address.LastIndexOf(':');
            string host = address.Substring(0, index);
            string p = address.Substring(index + 1);
            int port = int.Parse(p);
            return ToIPEndPoint(host, port);
        }

        private static HashSet<AddressFamily> GetSupportAddressFamily(string address)
        {
            var result = new HashSet<AddressFamily>();
            
            // Get server related information.
            IPHostEntry heserver = Dns.GetHostEntry(address);
            foreach (IPAddress ip in heserver.AddressList)
            {
                result.Add(ip.AddressFamily);
            }
            return result;
        }
    }
}