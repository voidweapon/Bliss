using System;

namespace Bliss
{
    public class MessageOpcodeAttribute : Attribute
    {
        public ushort Opcode { get; private set; }

        public MessageOpcodeAttribute(ushort opcode)
        {
            Opcode = opcode;
        }
    }
}