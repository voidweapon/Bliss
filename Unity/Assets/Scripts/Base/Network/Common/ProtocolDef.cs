namespace Bliss.Core
{
    public static class ProtocolDef
    {
        public const int PacketMinSize = 2;
        public const int PacketMaxSize = ushort.MaxValue * 16;
    }
}