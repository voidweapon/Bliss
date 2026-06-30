using System;
using Bliss.Core;
using Bliss.Pool;
using Bliss.Timer;
using ET;

namespace Bliss
{
    [EntitySystemOf(typeof(ProcessInnerMessageProxy))]
    public static partial class ProcessInnerMessageProxySystems
    {
        private static void Awake(this ProcessInnerMessageProxy self)
        {
            var root = self.LogicScene.Root;
            ProcessMessageManager.Instance.Register(root.Id);
        }
        private static void Destroy(this ProcessInnerMessageProxy self)
        {
            var root = self.LogicScene.Root;
            ProcessMessageManager.Instance.Unregister(root.Id);
        }
        private static void Update(this ProcessInnerMessageProxy self)
        {
            self.Messages.Clear();
            var root = self.LogicScene.Root;
            ProcessMessageManager.Instance.Fetch(root.Id, self.Messages, 1000);

            foreach (var message in self.Messages)
            {
                self.DistributeMessage(in message);
            }
        }
    
    
        public static void Send(this ProcessInnerMessageProxy self, ActorAddress targetAddress, MessageObject message)
        {
            self.SendInner(targetAddress, message);
        }

        private static bool SendInner(this ProcessInnerMessageProxy self, ActorAddress targetAddress, MessageObject message)
        {
            var root = self.LogicScene.Root;
            if (root.Process != targetAddress.Process)
            {
                throw new Exception($"can't send message to other process, from: {root.Process} to: {targetAddress.Process}");
            }
        
            LogMsg.Instance.DebugInnerMsg(self, targetAddress, message, true);
            return ProcessMessageManager.Instance.Enqueue(root.RootAddress, targetAddress, message);
        }

        public static void Reply(this ProcessInnerMessageProxy self, RootAddress fromAddress, IResponse response)
        {
            self.SendInner(new ActorAddress(fromAddress, 0), (MessageObject)response);
        }
    
        public static async ETTask<IResponse> Call(this ProcessInnerMessageProxy self, ActorAddress targetAddress, IRequest request, bool needException = true)
        {
            var root = self.LogicScene.Root;
            if (root.Process != targetAddress.Process)
            {
                throw new Exception($"can't send message to other process, from: {root.Process} to: {targetAddress.Process}");
            }
        
            var responseType = MessageOpcodeManager.Instance.GetResponseTypeBy(request.GetType());
        
            int rpcId = ++self.RpcIdGenerator;
            var rpcInfo = new ProcessInnerPrcInfo(targetAddress, request.GetType(), needException);
            self.RpcInfos.Add(rpcId, rpcInfo);
            request.RpcId = rpcId;

            if (!self.SendInner(targetAddress, (MessageObject)request))
            {
                IResponse errorResponse = (IResponse)ObjectPool.Instance.Fetch(responseType);
                errorResponse.Error = ErrorCode.ERR_ACTOR_NOT_FOUND;
                errorResponse.RpcId = rpcId;
                return errorResponse;
            }

            async ETTask Timeout()
            {
                await self.Scene().GetComponent<TimerComponent>().WaitDurationAsync(ProcessInnerMessageProxy.TimeOutMS);

                if (!self.RpcInfos.Remove(rpcId, out var action))
                {
                    return;
                }
                
                if (needException)
                {
                    action.RpcInfo.SetException(new Exception($"actor sender timeout: {request.GetType().FullName}"));
                }
                else
                {
                    IResponse errorResponse = (IResponse)ObjectPool.Instance.Fetch(responseType);
                    errorResponse.Error = ErrorCode.ERR_TASK_TIMEOUT;
                    errorResponse.RpcId = rpcId;
                    action.RpcInfo.SetResult(errorResponse);
                }
            }
        
            long beginTime = TimeInfo.Instance.ServerFrameTime();
        
            Timeout().Coroutine();
            var response = await rpcInfo.RpcInfo.Wait();
        
            long endTime = TimeInfo.Instance.ServerFrameTime();
        
            long costTime = endTime - beginTime;
            if (costTime > 200)
            {
                Log.Warning($"actor rpc time > 200: {costTime} {request.GetType().FullName}");
            }
        
            return response;
        }

        /// <summary>
        /// 处理收到的内网消息
        /// </summary>
        /// <param name="self"></param>
        /// <param name="messageInfo"></param>
        private static void DistributeMessage(this ProcessInnerMessageProxy self, in ProcessMessageInfo messageInfo)
        {
            LogMsg.Instance.DebugInnerMsg(self, messageInfo.TargetAddress, messageInfo.MessageObject, false);
            if (messageInfo.MessageObject is IResponse response)
            {
                self.HandleResponse(response);
            }
            else
            {
                self.HandleMessage(messageInfo);
            }
        }
    

        private static void HandleMessage(this ProcessInnerMessageProxy self, in ProcessMessageInfo messageInfo)
        {
            ActorAddress actorAddress = messageInfo.TargetAddress;
            MessageObject messageObject = messageInfo.MessageObject;


            var mailboxCmp = self.RootScene().GetComponent<NetworkEntityManagerComponent>().GetEntityMailBox(actorAddress.InstanceId);
            if (mailboxCmp == null)
            {
                if(messageObject is IRequest request)
                {
                    LogMsg.Instance.DebugInnerMsg(self, messageInfo.MessageObject, $"HandleMessage Not Found Mailbox[{actorAddress.InstanceId}]");
                    
            
                    var responseType = MessageOpcodeManager.Instance.GetResponseTypeBy(request.GetType());
                    IResponse errorResponse = (IResponse)ObjectPool.Instance.Fetch(responseType);
                    errorResponse.Error = ErrorCode.ERR_ACTOR_NOT_FOUND;
                    errorResponse.RpcId = request.RpcId;
                
                    self.Reply(actorAddress.RootAddress, errorResponse);
                }
                return;
            }
            LogMsg.Instance.DebugInnerMsg(self, messageInfo.MessageObject, $"HandleMessage Mailbox[{actorAddress.InstanceId}]");
            mailboxCmp.Delivery(actorAddress.RootAddress, messageObject);
        }
    
    
        private static void HandleResponse(this ProcessInnerMessageProxy self,  IResponse response)
        {
            if (!self.RpcInfos.Remove(response.RpcId, out var rpcInfo))
            {
                return;
            }

            if (response.Error == ErrorCode.ERR_TASK_TIMEOUT)
            {
                rpcInfo.RpcInfo.SetException(new RpcException(response.Error, $"Rpc error: request, 注意Actor消息超时，请注意查看是否死锁或者没有reply: actorId: {rpcInfo.ActorAddress} {rpcInfo.RequestType.FullName}, response: {response.GetType().FullName} error: {response.Error}"));
                return;
            }
        
            if(rpcInfo.NeedException && ErrorCode.IsRpcNeedThrowException(response.Error))
            {
                rpcInfo.RpcInfo.SetException(new RpcException(response.Error, $"Rpc error: actorId: {rpcInfo.ActorAddress} request: {rpcInfo.RequestType.FullName}, response: {response.GetType().FullName} error: {response.Error}"));
                return;
            }
        
            rpcInfo.RpcInfo.SetResult(response);
        }
    
    }
}