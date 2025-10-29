using System;
using Bliss.Core;

namespace Bliss
{
    public class MessageOpcodeAttribute : BaseAttribute
    {
        public ushort Opcode { get; private set; }

        public MessageOpcodeAttribute(ushort opcode)
        {
            Opcode = opcode;
        }
    }
}