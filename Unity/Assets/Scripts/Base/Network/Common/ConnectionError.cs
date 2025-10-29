namespace Bliss.Core
{
    public static class ConnectionError
    {
        /// <summary>
        /// 远端已经断开连接
        /// </summary>
        public const int ERR_PEER_DISCONNECT = 100207;
        
        public const int ERR_SOCKET_CANT_SEND = 100208;
        
        public const int ERR_SOCKET_CANT_RECEIVE = 100209;
        /// <summary>
        /// 收到无效包
        /// </summary>
        public const int ERR_INVALID_PACKET = 100210;
        
        /// <summary>
        /// 用户的SendBuff积累过大
        /// </summary>
        public const int ERR_SENDBUFF_TOOLARGE = 100211;
        
        /// <summary>
        /// 用户的ReceiveBuff积累过大
        /// </summary>
        public const int ERR_RECEIVEBUFF_TOOLARGE = 100212;
        
        public const int ERR_DEAD_LINK = 100213;
        
        /// <summary>
        /// 握手超时
        /// </summary>
        public const int ERR_HANDSHAKE_TIMEOUT = 100214;
        
        public const int ERR_RpcFail = 100215;
        
    }
}