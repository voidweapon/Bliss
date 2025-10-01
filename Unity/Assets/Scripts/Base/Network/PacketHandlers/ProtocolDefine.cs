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
        /// <summary>
        /// 普通消息头长度
        /// </summary>
        public const int MsgPacketHeaderSize = sizeof(byte) + sizeof(bool) + sizeof(uint) + sizeof(uint);
        
        /// <summary>
        /// 关闭连接消息头长度
        /// </summary>
        public const int FinPacketHeaderSize = MsgPacketHeaderSize +  sizeof(int);
    }
}