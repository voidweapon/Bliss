using System;
using System.Collections.Generic;
using System.Net;
using Bliss.Core.Network;
using ET;

namespace Bliss
{
    public  sealed class Session : Entity, IAwake<NetworkComponent>, IDestroy
    {
        public NetworkComponent NetworkComponent;
        public IPEndPoint RemoteEndPoint;
        /// <summary>Whether this session carries addressed Server-internal messages.</summary>
        public bool IsInternal;
        public int ErrorCode;
        public int RPCId;
        public long LastReceiveTime;
        public long LastSendTime;
        public Dictionary<int, RpcInfo> RpcInfos = new Dictionary<int, RpcInfo>();
    }
}