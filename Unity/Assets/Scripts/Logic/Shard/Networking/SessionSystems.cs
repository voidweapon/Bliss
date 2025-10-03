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
        
        public static void Send(this Session self, MessageObject message)
        {
            var networkComponent = self.Parent as NetworkComponent;
            SocketBufferRef netStreamRef = MessageSerializer.MessageToNetStream(networkComponent.NetDriver,  message);
            self.SendRaw(netStreamRef);
        }
        
        public static void SendRaw(this Session self, SocketBufferRef buffer)
        {
            var networkComponent = self.Parent as NetworkComponent;
            networkComponent.Send((uint)self.Id, buffer);
        }

        public static async ETTask<IResponse> Call(this Session self, MessageObject request, ETCancellationToken cancellation)
        {
            var requestType = MessageOpcodeManager.Instance.GetRequestTypeBy(request.GetType());
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
                IResponse response = await rpcInfo.Wait();
                return response;
            }
            finally
            {
                cancellation.Remove(CancelAction);   
            }
        }
        
        public static async ETTask<IResponse> Call(this Session self, IRequest request, long timeMS, ETCancellationToken cancellation)
        {
            var requestType = MessageOpcodeManager.Instance.GetRequestTypeBy(request.GetType());
            int rpcId = self.GenRpcId();
            var rpcInfo = new RpcInfo(rpcId);
            self.RpcInfos.Add(rpcId, rpcInfo);
            self.Send((MessageObject)request);
            
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
                IResponse response = await rpcInfo.Wait();
                return response;
            }
            finally
            {
                cancellation.Remove(CancelAction);   
            }
        }
        
        internal static void OnResponse(this Session self, IResponse response)
        {
            if (!self.RpcInfos.Remove(response.RpcId, out RpcInfo action))
            {
                return;
            }
            action.SetResult(response);
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