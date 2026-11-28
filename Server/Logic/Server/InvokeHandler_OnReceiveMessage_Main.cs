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
            // The wire address carries the sender process and the target Root and instance.
            actorAddress.Process = session.Root().Process;
            
            var processInnerSender = session.RootScene().GetComponent<ProcessInnerMessageProxy>();
            object message = args.Message;
            if (message is IResponse response)
            {
                if (!args.ReceivedBeforeRootReady && session.Root().InitializationState == ERootInitializationState.Ready
                    || session.RpcInfos.TryGetValue(response.RpcId, out RpcInfo rpc)
                    && MessageOpcodeManager.Instance.GetResponseTypeBy(rpc.RequestType) == response.GetType())
                {
                    session.OnResponse(response);
                }
                return;
            }

            ERootInitializationState state = session.Root().InitializationState;
            bool rejectAtMain = false;
            if (args.ReceivedBeforeRootReady || state == ERootInitializationState.Initializing
                || state == ERootInitializationState.Failed)
            {
                RootInitializationMessagePolicy policy = args.ReceivedBeforeRootReady
                    ? args.InitializationMessagePolicy : session.Root().InitializationMessagePolicy;
                bool isRpcRequest = message is IRequest && message is not ILocationMessage;
                rejectAtMain = isRpcRequest || state == ERootInitializationState.Failed || policy == null
                    || !policy.Allows(message.GetType(), ERootMessageIngress.Internal);
            }

            RootMessageAdmission admission = args.TargetAdmission;
            RootMessageAdmission currentAdmission = ProcessMessageManager.Instance.CaptureAdmission(actorAddress.Root);
            int rejectionError = ErrorCode.ERR_Success;
            if (rejectAtMain)
            {
                rejectionError = ErrorCode.ERR_ROOT_NOT_READY;
            }
            else if (admission == null || currentAdmission == null || admission.Root != currentAdmission.Root)
            {
                rejectionError = ErrorCode.ERR_ACTOR_NOT_FOUND;
            }
            else if (!admission.HasRoute)
            {
                rejectionError = admission.Snapshot.State == ERootInitializationState.Initializing
                    || admission.Snapshot.State == ERootInitializationState.Failed
                    ? ErrorCode.ERR_ROOT_NOT_READY : ErrorCode.ERR_ACTOR_NOT_FOUND;
            }
            else if (admission.Snapshot.State == ERootInitializationState.Failed
                || admission.Snapshot.State == ERootInitializationState.Initializing
                && (message is IRequest && message is not ILocationMessage
                    || !admission.Snapshot.MessagePolicy.Allows(
                        message.GetType(), ERootMessageIngress.Internal)))
            {
                rejectionError = ErrorCode.ERR_ROOT_NOT_READY;
            }
            else if (!currentAdmission.HasRoute)
            {
                rejectionError = ErrorCode.ERR_ACTOR_NOT_FOUND;
            }
            if (rejectionError != ErrorCode.ERR_Success)
            {
                if (message is IRequest request && message is not ILocationMessage)
                {
                    using MessageObject rejection = (MessageObject)MessageOpcodeManager.Instance.CreateResponseBy(
                        request.GetType(), request.RpcId, rejectionError);
                    actorAddress.Process = fromProcess;
                    session.Send(actorAddress, rejection);
                }
                else
                {
                    if (message is InternalMessageSendEnvelope envelope)
                    {
                        if (MessageOpcodeManager.Instance.GetMessageTypeBy(
                            envelope.PayloadOpcode, out Type businessMessageType))
                        {
                            string businessMessageName = businessMessageType.FullName ?? businessMessageType.Name;
                            Log.Warning(
                                $"Root {envelope.TargetAddress.RootAddress} rejected internal message {businessMessageName} from {envelope.OriginRoot}");
                        }
                        else
                        {
                            Log.Warning(
                                $"Root {envelope.TargetAddress.RootAddress} rejected internal message opcode {envelope.PayloadOpcode} from {envelope.OriginRoot}");
                        }
                        return;
                    }

                    RootAddress rejectedRoot = rejectAtMain ? session.Root().RootAddress : actorAddress.RootAddress;
                    Log.Warning($"Root {rejectedRoot} rejected internal message {message.GetType().FullName} from process {fromProcess} ({session.RemoteEndPoint})");
                }
                return;
            }
            switch (message)
            {
                case ILocationMessage:
                    processInnerSender.Send(actorAddress, (MessageObject)message);
                    break;
                case IRequest request:
                    HandleCall(session, actorAddress, request, fromProcess).Coroutine();
                    break;
                default:
                    processInnerSender.Send(actorAddress, (MessageObject)message);
                    break;
            }
        }

        private async ETTask HandleCall(Session session, ActorAddress targetAddress, IRequest request, int fromProcess)
        {
            var instanceId = session.InstanceId;
            var rpcId = request.RpcId;
            var processInnerSender = session.RootScene().GetComponent<ProcessInnerMessageProxy>();
            
            var response = await processInnerSender.Call(targetAddress, request, needException: false);
            
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