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
            SocketBufferRef netStreamRef = MessageSerializer.MessageToNetStream(networkComponent.NetDriver, message);
            self.SendRaw(netStreamRef, isReliable);
        }
        
        /// <summary>Sends a routed message to an Actor over an internal Session.</summary>
        /// <param name="self">The Session used to send the message.</param>
        /// <param name="actorAddress">The destination Actor address included in the internal header.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="isReliable">Whether the transport should send the packet reliably.</param>
        public static void Send(this Session self, ActorAddress actorAddress, MessageObject message, bool isReliable = true)
        {
            var networkComponent = self.NetworkComponent;
            SocketBufferRef netStreamRef = MessageSerializer.MessageToNetStream(networkComponent.NetDriver, actorAddress, message);
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

        /// <summary>Sends an RPC request and waits for its response or cooperative cancellation.</summary>
        /// <param name="self">The Session used to send the request.</param>
        /// <param name="request">The RPC request to send.</param>
        /// <param name="cancellation">The optional cooperative cancellation source.</param>
        /// <param name="needException">Whether failures are surfaced as exceptions when supported by this overload.</param>
        /// <returns>The response matching the request type.</returns>
        /// <exception cref="TaskAbortedException">The RPC was canceled.</exception>
        public static async ETTask<IResponse> Call(this Session self, IRequest request, ETCancellationToken cancellation, bool needException = true)
        {
            if (cancellation?.IsCancel() == true)
            {
                throw new TaskAbortedException("Session call canceled.", cancellation);
            }

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
                pendingRcp.SetException(new TaskAbortedException("Session call canceled.", cancellation));
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
        
        /// <summary>Sends an RPC request to an Actor and waits for its response, timeout, or cancellation.</summary>
        /// <param name="self">The Session used to send the request.</param>
        /// <param name="actorAddress">The destination Actor address included in the internal header.</param>
        /// <param name="request">The RPC request to send.</param>
        /// <param name="timeOutMS">The maximum time to wait, or a non-positive value for no timeout.</param>
        /// <param name="cancellation">The optional cooperative cancellation source.</param>
        /// <param name="needException">Whether timeout and cancellation are surfaced as exceptions instead of error responses.</param>
        /// <returns>The response matching the request type, including an error response when exceptions are disabled.</returns>
        /// <exception cref="TaskAbortedException">The RPC was canceled and exceptions are enabled.</exception>
        /// <exception cref="TaskTimeoutException">The RPC timed out and exceptions are enabled.</exception>
        public static async ETTask<IResponse> Call(this Session self, 
            ActorAddress actorAddress,
            IRequest request,
            long timeOutMS, 
            ETCancellationToken cancellation = null, bool needException = true)
        {
            if (cancellation?.IsCancel() == true)
            {
                if (needException)
                {
                    throw new TaskAbortedException("Session call canceled.", cancellation);
                }

                Type canceledResponseType = MessageOpcodeManager.Instance.GetResponseTypeBy(request.GetType());
                IResponse canceledResponse = (IResponse)ObjectPool.Instance.Fetch(canceledResponseType);
                request.RpcId = self.GenRpcId();
                canceledResponse.Error = ErrorCode.ERR_TASK_ABORTED;
                canceledResponse.RpcId = request.RpcId;
                return canceledResponse;
            }

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
                    pendingRcp.SetException(new TaskAbortedException("Session call canceled.", cancellation));
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
                    if (!isTimeOut)
                    {
                        return;
                    }

                    if (!self.RpcInfos.Remove(rpcId, out var timeoutRpc))
                    {
                        return;
                    }
                    
                    if (needException)
                    {
                        timeoutRpc.SetException(new TaskTimeoutException($"Session call timed out: requestType={responseType} rpcId={rpcId} timeMS={timeOutMS}."));
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