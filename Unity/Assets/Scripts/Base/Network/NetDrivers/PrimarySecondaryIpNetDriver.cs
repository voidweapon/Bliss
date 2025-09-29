using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Bliss.Core.Network
{
    public class PrimarySecondaryIpNetDriver : IpNetDriver
    {
       private List<IpNetDriver> _drivers =  new List<IpNetDriver>();
       //private int _curDriverIndex = 0;

       protected override void OnDispose()
       {
           throw new System.NotImplementedException();
       }

       public override void InitClient(AddressFamily addressFamily = AddressFamily.InterNetwork)
       {
           throw new System.NotImplementedException();
       }

       public override void InitServer(IPEndPoint endPoint)
       {
           throw new System.NotImplementedException();
       }

       protected override void CreateConnection(uint connectionId, IPEndPoint remoteEndPoint)
       {
           throw new System.NotImplementedException();
       }

       public override void Send(uint connectionId, SocketBufferRef socketBufferRef)
       {
           throw new System.NotImplementedException();
       }

       public override void RemoveConnection(uint connectionId, int error = 0)
       {
           throw new System.NotImplementedException();
       }
       
    }
}