using System;
using Bliss.Core.EventSystem;
using ET;

namespace Bliss
{
    /// <summary>
    /// 内网消息路由
    /// </summary>
    [Invoke((long)ELogicSceneType.Main)]
    public class InvokeHandler_OnReceiveMessage_Main : AInvokeHandler<Invoke_OnReceiveMessage>
    {
        public override void Handle(Invoke_OnReceiveMessage args)
        {
            Session session = args.Session;
            ActorAddress  actorAddress = args.TargetAddress;
            
            int fromProcess = actorAddress.Process;
            //actorAddress Process在发生时更改为发送进程的id，收到消息时更改为当前进程的id
            actorAddress.Process = session.Root().Process;
            
            var processInnerSender = session.RootScene().GetComponent<ProcessInnerMessageProxy>();
            object message = args.Message;
            switch (message)
            {
                case IRequest request:
                    HandleCall(session, actorAddress, request, fromProcess).Coroutine();
                    break;
                default:
                    processInnerSender.Send(args.TargetAddress, (MessageObject)message);
                    break;
            }
        }

        private async ETTask HandleCall(Session session, ActorAddress targetAddress, IRequest request, int fromProcess)
        {
            var instanceId = session.InstanceId;
            var rpcId = request.RpcId;
            var processInnerSender = session.RootScene().GetComponent<ProcessInnerMessageProxy>();
            
            var response = await processInnerSender.Call(targetAddress, request);
            
            targetAddress.Process = fromProcess;
            response.RpcId = rpcId;
            
            if (instanceId != session.InstanceId)
            {
                return;
            }
            session.Send(targetAddress, (MessageObject)response);
        }
    }
}