using System;
using Bliss.Core;
using ET;

namespace Bliss;

internal static class InternalMessageGatewayLogStages
{
    internal const string RECEIVE_SEND = "receive-send";
    internal const string RETURN_RESPONSE = "return-response";
    internal const string DISPATCH_REQUEST = "dispatch-request";
    internal const string DISPATCH_RESPONSE = "dispatch-response";
    internal const string SEND_ONE_WAY = "send-one-way";
    internal const string SEND_REQUEST = "send-request";
}

/// <summary>Provides the Main process address and actor identity for the internal gateway.</summary>
public static class InternalMessageGateway
{
    /// <summary>The configured Root identifier used by Main in every process.</summary>
    public const int ROOT_ID = 0;
    internal const int UNSET_ORIGIN_RPC_ID = 0;
}

/// <summary>Forwards one-way business envelopes through the Main process gateway.</summary>
[MessageHandler(ELogicSceneType.Main)]
public sealed class InternalMessageSendEnvelopeHandler :
    MessageHandler<LogicScene, InternalMessageSendEnvelope>
{
    protected override async ETTask Run(LogicScene main, InternalMessageSendEnvelope envelope)
    {
        LogMsg.Instance.DebugInternalGateway(envelope.OriginRoot, envelope.TargetAddress,
            envelope.PayloadOpcode, InternalMessageGatewayLogStages.RECEIVE_SEND,
            InternalMessageGateway.UNSET_ORIGIN_RPC_ID);

        if (envelope.TargetAddress.Process != main.Root.Process)
        {
            MessageSender sender = main.GetComponent<MessageSender>();
            Session session = sender.GetSession(envelope.TargetAddress.Process);
            ActorAddress mainAddress = getMainAddress(main.Root.Process);
            session.Send(mainAddress, envelope);
            await ETTask.CompletedTask;
            return;
        }

        MessageObject message = MessageSerializer.DeserializePayload(envelope.PayloadOpcode, envelope.Payload);
        if (!ProcessMessageManager.Instance.Enqueue(envelope.OriginRoot, envelope.TargetAddress, message))
        {
            Log.Warning(
                $"Root {envelope.TargetAddress.RootAddress} could not route internal message {message.GetType().FullName} from {envelope.OriginRoot}");
        }

        await ETTask.CompletedTask;
    }

    private static ActorAddress getMainAddress(int sourceProcess)
    {
        return new ActorAddress(new RootAddress(sourceProcess, InternalMessageGateway.ROOT_ID));
    }
}

/// <summary>Forwards RPC envelopes through Main and returns the business response to the caller.</summary>
[MessageHandler(ELogicSceneType.Main)]
public sealed class InternalMessageCallEnvelopeHandler :
    MessageHandler<LogicScene, InternalMessageCallEnvelope, InternalMessageCallResponse>
{
    protected override async ETTask Run(LogicScene main, InternalMessageCallEnvelope envelope,
        InternalMessageCallResponse response)
    {
        int originRpcId = envelope.OriginRpcId == InternalMessageGateway.UNSET_ORIGIN_RPC_ID
            ? envelope.RpcId
            : envelope.OriginRpcId;
        if (envelope.TargetAddress.Process != main.Root.Process)
        {
            envelope.OriginRpcId = originRpcId;
            MessageSender sender = main.GetComponent<MessageSender>();
            Session session = sender.GetSession(envelope.TargetAddress.Process);
            ActorAddress mainAddress = new ActorAddress(
                new RootAddress(main.Root.Process, InternalMessageGateway.ROOT_ID));
            IResponse forwarded = await session.Call(mainAddress, envelope,
                Options.Instance.InternalMessageForwardTimeoutMS, null, needException: false);
            if (forwarded is not InternalMessageCallResponse forwardedEnvelope)
            {
                throw new InvalidOperationException(
                    $"Unexpected internal gateway response: {forwarded.GetType().FullName}.");
            }

            copyResponse(forwardedEnvelope, response);
            LogMsg.Instance.DebugInternalGateway(envelope.OriginRoot, envelope.TargetAddress,
                response.PayloadOpcode, InternalMessageGatewayLogStages.RETURN_RESPONSE, originRpcId);
            return;
        }

        MessageObject payload = MessageSerializer.DeserializePayload(envelope.PayloadOpcode, envelope.Payload);
        if (payload is not IRequest request || payload is ILocationMessage)
        {
            response.Error = ErrorCode.ERR_RPC_FAIL;
            Log.Warning($"Main rejected invalid internal RPC payload {payload.GetType().FullName} for {envelope.TargetAddress}");
            return;
        }

        LogMsg.Instance.DebugInternalGateway(envelope.OriginRoot, envelope.TargetAddress,
            envelope.PayloadOpcode, InternalMessageGatewayLogStages.DISPATCH_REQUEST, envelope.OriginRpcId);
        IResponse businessResponse = await main.GetComponent<ProcessInnerMessageProxy>()
            .Call(envelope.TargetAddress, request, Options.Instance.InternalMessageForwardTimeoutMS,
                needException: false);
        businessResponse.RpcId = originRpcId;
        response.Payload = MessageSerializer.SerializePayload((MessageObject)businessResponse,
            out ushort responseOpcode);
        response.PayloadOpcode = responseOpcode;
        response.Error = businessResponse.Error;
        response.Message = businessResponse.Message;
        LogMsg.Instance.DebugInternalGateway(envelope.OriginRoot, envelope.TargetAddress,
            responseOpcode, InternalMessageGatewayLogStages.DISPATCH_RESPONSE, envelope.OriginRpcId);
    }

    private static void copyResponse(InternalMessageCallResponse source, InternalMessageCallResponse target)
    {
        target.PayloadOpcode = source.PayloadOpcode;
        target.Payload = source.Payload;
        target.Error = source.Error;
        target.Message = source.Message;
    }
}
