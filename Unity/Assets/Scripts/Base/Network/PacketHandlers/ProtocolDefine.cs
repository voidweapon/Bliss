namespace Bliss.Core.Network
{
    public enum EProtocolType : byte
    {
        Handshake = 1,
        Msg = 2,
        Fin = 3,
    }

    public static class ProtocolDefine
    {
        public const int MsgPacketHeaderSize = sizeof(byte) + sizeof(bool) + sizeof(uint) + sizeof(uint);
    }
}