using System;
using Bliss.Core;
using Bliss.Pool;
using Bliss.Timer;
using ET;

namespace Bliss
{
    [EntitySystemOf(typeof(Session))]
    public static partial class SessionSystem
    {
        private static void Awake(this Session self, NetworkComponent network)
        {
            self.NetworkComponent = network;
            self.LastReceiveTime = self.LastSendTime = TimeInfo.Instance.ClientNow();
        }
        private static void Destroy(this Session self)
        {
            var networkComponent = self.NetworkComponent;
            networkComponent?.RemoveConnection((uint)self.Id, self.ErrorCode);
            self.NetworkComponent =  null;
        }

        public static SocketBufferRef MessageToNetStream(this Session self, MessageObject message)
        {
            var networkComponent = self.NetworkComponent;
            SocketBufferRef netStreamRef = MessageSerializer.MessageToNetStream(networkComponent.NetDriver,  message);
            return netStreamRef;
        }
        
        public static void Send(this Session self, MessageObject message, bool isReliable = true)
        {
            var networkComponent = self.NetworkComponent;
            SocketBufferRef netStreamRef = MessageSerializer.MessageToNetStream(networkComponent.NetDriver,  message);
            self.SendRaw(netStreamRef, isReliable);
        }
        
        public static void Send(this Session self, ActorAddress actorAddress, MessageObject message, bool isReliable = true)
        {
            var networkComponent = self.NetworkComponent;
            SocketBufferRef netStreamRef = MessageSerializer.MessageToNetStream(networkComponent.NetDriver,  message);
            self.SendRaw(netStreamRef, isReliable);
        }
        
        public static void SendRawCopy(this Session self, SocketBufferRef buffer, bool isReliable = true)
        {
            var networkComponent = self.NetworkComponent;
            var newBuffer = networkComponent.NetDriver.RentBuffer(buffer.Target.TrueLength);
            newBuffer.Target.Clone(buffer.Target);
            networkComponent.Send((uint)self.Id, newBuffer, isReliable);
        }
        
        public static void SendRaw(this Session self, SocketBufferRef buffer, bool isReliable = true)
        {
            var networkComponent = self.NetworkComponent;
            networkComponent.Send((uint)self.Id, buffer, isReliable);
        }

        public static async ETTask<IResponse> Call(this Session self, IRequest request, ETCancellationToken cancellation, bool needException = true)
        {
            var responseType = MessageOpcodeManager.Instance.GetResponseTypeBy(request.GetType());
            int rpcId = self.GenRpcId();
            var rpcInfo = new RpcInfo(request.GetType());
            self.RpcInfos.Add(rpcId, rpcInfo);
            request.RpcId = rpcId;
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

            try
            {
                cancellation?.Add(CancelAction);
                IResponse response = await rpcInfo.Wait();
                return response;
            }
            finally
            {
                cancellation?.Remove(CancelAction);   
            }
        }
        
        public static async ETTask<IResponse> Call(this Session self, 
            ActorAddress actorAddress,
            IRequest request,
            long timeOutMS, 
            ETCancellationToken cancellation = null, bool needException = true)
        {
            var responseType = MessageOpcodeManager.Instance.GetResponseTypeBy(request.GetType());
            int rpcId = self.GenRpcId();
            var rpcInfo = new RpcInfo(request.GetType());
            self.RpcInfos.Add(rpcId, rpcInfo);
            request.RpcId = rpcId;
            self.Send(actorAddress, (MessageObject)request);
            
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

                if (needException)
                {
                    pendingRcp.SetException(new TaskAbortedException());
                }
                else
                {
                    IResponse response = (IResponse)ObjectPool.Instance.Fetch(responseType);
                    response.Error = ErrorCode.ERR_TASK_ABORTED;
                    response.RpcId = rpcId;
                    pendingRcp.SetResult(response);
                }
                
            }

            if (timeOutMS > 0)
            {
                async ETTask Timeout()
                {
                    long instanceId = self.InstanceId;
                    bool isTimeOut = await self.RootScene().GetComponent<TimerComponent>().WaitDurationAsync(timeOutMS, cancellation);
 
                    if (self.IsDisposed || instanceId != self.InstanceId)
                    {
                        return;
                    }
                    if (!self.RpcInfos.Remove(rpcId, out var timeoutRpc))
                    {
                        return;
                    }

                    if (!isTimeOut)
                    {
                        return;
                    }
                    
                    if (needException)
                    {
                        timeoutRpc.SetException(new TaskCanceledException($"session call timeout: requestType={responseType} rpcId={rpcId} timeMS={timeOutMS}"));
                    }
                    else
                    {
                        IResponse response = (IResponse)ObjectPool.Instance.Fetch(responseType);
                        response.Error = ErrorCode.ERR_TASK_TIMEOUT;
                        response.RpcId = rpcId;
                        timeoutRpc.SetResult(response);
                    }
                }
                Timeout().Coroutine();
            }

            try
            {
                cancellation?.Add(CancelAction);
                IResponse response = await rpcInfo.Wait();
                return response;
            }
            finally
            {
                cancellation?.Remove(CancelAction);   
            }
        }
        
        public static void OnResponse(this Session self, IResponse response)
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