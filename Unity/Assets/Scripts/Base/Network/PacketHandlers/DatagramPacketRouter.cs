using System;
using System.Net;

namespace Bliss.Core.Network
{
    public class DatagramPacketRouter
    {
        private UdpNetDriver _udpDriver;
        public DatagramPacketRouter(UdpNetDriver  udpDriver)
        {
            _udpDriver = udpDriver;
        }

        public int Send(byte[] buffer, int offset, int count, bool reliable)
        {
            throw new NotImplementedException();
        }

        public void Output()
        {
            throw new NotImplementedException();
        }
        
        
        public void Input(byte[] buffer, int offset, int count, EndPoint  remoteEndPoint)
        {
            //小于最底层协议的包头大小，是非法消息
            if (count < 1)
            {
                return;
            }
            
            byte reliable = buffer[offset];
            if (reliable == 0)
            {
                
            }
            else
            {
                
            }
        }


        public void Receive()
        {
            throw new NotImplementedException();
        }
        
    }
}