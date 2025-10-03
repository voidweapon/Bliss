using System;
using System.Collections.Generic;
using System.Net;
using Bliss.Core.Network;
using ET;

namespace Bliss
{
    public  sealed class Session : Entity, IAwake, IDestroy
    {
        public IPEndPoint RemoteEndPoint;
        public int ErrorCode;
        public int RPCId;
        public long LastReceiveTime;
        public long LastSendTime;
        public Dictionary<int, RpcInfo> RpcInfos = new Dictionary<int, RpcInfo>();
    }

    public readonly struct RpcInfo
    {
        public readonly int RpcId;
        private readonly ETTask<IResponse> tcs;

        public RpcInfo(int rpcId)
        {
            RpcId = rpcId;
            tcs = ETTask<IResponse>.Create(true);
        }
        
        public void SetResult(IResponse response)
        {
            tcs.SetResult(response);
        }

        public void SetException(Exception exception)
        {
            tcs.SetException(exception);
        }

        public async ETTask<IResponse> Wait()
        {
            return await tcs;
        }
    }
}