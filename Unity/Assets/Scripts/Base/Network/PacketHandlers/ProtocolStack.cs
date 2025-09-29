using System.Collections.Generic;

namespace Bliss.Core.Network
{
    public class ProtocolStack
    {
        private List<AProtocolUnit> _stack = new List<AProtocolUnit>();

        public void Push(AProtocolUnit protoUnit)
        {
            _stack.Add(protoUnit);
        }

        public int Send()
        {
            return 0;
        }


        public void Receive()
        {
            
        }
    }
}