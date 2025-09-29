using System;
using Bliss.Core;
using Bliss.Timer;
using ET;

namespace Bliss
{
    [EntitySystemOf(typeof(Session))]
    public static partial class SessionSystem
    {
        public static void Awake(this Session self)
        {
            self.LastReceiveTime = self.LastSendTime = TimeInfo.Instance.ClientNow();
        }
        public static void Destroy(this Session self)
        {
            var networkComponent = self.Parent as NetworkComponent;
            networkComponent.RemoveConnection((uint)self.Id, self.ErrorCode);
        }


        public static void Send(this Session self, object message)
        {
            
        }
        
        public static void SendRaw(this Session self, SocketBufferRef buffer)
        {
            var networkComponent = self.Parent as NetworkComponent;
            networkComponent.Send((uint)self.Id, buffer);
        }

        public static async ETTask<object> Call(this Session self, object request, ETCancellationToken cancellation)
        {
            int rpcId = self.GenRpcId();
            var rpcInfo = new RpcInfo(rpcId);
            self.RpcInfos.Add(rpcId, rpcInfo);
            self.Send(request);

            long instanceId = self.InstanceId;
            void CancelAction()
            {
                if (self.IsDisposed || instanceId != self.InstanceId)
                {
                    return;
                }
                if (!self.RpcInfos.Remove(rpcId, out var pendingRcp))
                {
                    return;
                }
                pendingRcp.SetException(new TaskAbortedException());   
            }

            try
            {
                cancellation.Add(CancelAction);
                object response = await rpcInfo.Wait();
                return response;
            }
            finally
            {
                cancellation.Remove(CancelAction);   
            }
        }
        
        public static async ETTask<object> Call(this Session self, object request, long timeMS, ETCancellationToken cancellation)
        {
            int rpcId = self.GenRpcId();
            var requestType = request.GetType();
            var rpcInfo = new RpcInfo(rpcId);
            self.RpcInfos.Add(rpcId, rpcInfo);
            self.Send(request);
            
            long instanceId = self.InstanceId;
            void CancelAction()
            {
                if (self.IsDisposed || instanceId != self.InstanceId)
                {
                    return;
                }
                if (!self.RpcInfos.Remove(rpcId, out var pendingRcp))
                {
                    return;
                }
                pendingRcp.SetException(new TaskAbortedException());   
            }

            if (timeMS > 0)
            {
                async ETTask Timeout()
                {
                    long instanceId = self.InstanceId;
                    await self.Root().GetComponent<TimerComponent>().WaitDurationAsync(timeMS, cancellation);
 
                    if (self.IsDisposed || instanceId != self.InstanceId)
                    {
                        return;
                    }
                    if (!self.RpcInfos.Remove(rpcId, out var timeoutRpc))
                    {
                        return;
                    }
                    timeoutRpc.SetException(new TaskCanceledException($"session call timeout: requestType={requestType} rpcId={rpcId} timeMS={timeMS}"));
                }
                Timeout().Coroutine();
            }

            try
            {
                cancellation.Add(CancelAction);
                object response = await rpcInfo.Wait();
                return response;
            }
            finally
            {
                cancellation.Remove(CancelAction);   
            }
        }

        private static int GenRpcId(this Session self)
        {
            int rpcId = RandomGenerator.RandInt32();
            int tryLimit = 100;
            int tryCount = 0;
            while (self.RpcInfos.ContainsKey(rpcId))
            {
                rpcId = RandomGenerator.RandInt32();
                tryCount++;
                if (tryCount > tryLimit)
                {
                    break;
                }
            }

            if (tryCount > tryLimit)
            {
                throw new Exception($"gen rpc id failed during {tryCount} try limit {tryLimit}");
            }
            
            return rpcId;
        }
    }
}