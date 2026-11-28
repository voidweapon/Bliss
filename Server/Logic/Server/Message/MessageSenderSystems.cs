using System;
using Bliss.Core;
using ET;

namespace Bliss;

/// <summary>Routes business messages locally or through the owning Main process.</summary>
public static class MessageSenderSystems
{
    /// <summary>Sends a business message locally or forwards it through Main to another process.</summary>
    /// <param name="self">The sender bound to the originating Root.</param>
    /// <param name="actorAddress">The destination Actor.</param>
    /// <param name="message">The business message to send.</param>
    /// <exception cref="ArgumentNullException">The message is null.</exception>
    /// <exception cref="ArgumentException">The message is not a message object.</exception>
    /// <exception cref="InvalidOperationException">The business message has no registered opcode.</exception>
    public static void Send(this MessageSender self, ActorAddress actorAddress, IMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message is not MessageObject msgObj)
        {
            throw new ArgumentException("The business message must be a MessageObject.", nameof(message));
        }
        
        //同进程消息
        if(actorAddress.Process == self.Root().Process)
        {
            self.RootScene().GetComponent<ProcessInnerMessageProxy>().Send(actorAddress, msgObj);
            return;
        }
        
        InternalMessageSendEnvelope envelope = new()
        {
            OriginRoot = self.Root().RootAddress,
            TargetAddress = actorAddress,
            Payload = MessageSerializer.SerializePayload(msgObj, out ushort opcode),
            PayloadOpcode = opcode,
        };
        LogMsg.Instance.DebugInternalGateway(envelope.OriginRoot, envelope.TargetAddress,
            envelope.PayloadOpcode, InternalMessageGatewayLogStages.SEND_ONE_WAY,
            InternalMessageGateway.UNSET_ORIGIN_RPC_ID);
        self.RootScene().GetComponent<ProcessInnerMessageProxy>()
            .Send(new ActorAddress(new RootAddress(self.Root().Process, InternalMessageGateway.ROOT_ID)), envelope);
    }
    
    
    /// <summary>Sends an RPC locally or forwards it through Main to another process.</summary>
    /// <param name="self">The sender bound to the originating Root.</param>
    /// <param name="actorAddress">The destination Actor.</param>
    /// <param name="request">The RPC request to send.</param>
    /// <param name="timeOutMs">The maximum time to wait for the response.</param>
    /// <param name="needException">Whether RPC error responses should be raised as exceptions.</param>
    /// <returns>The response returned by the destination Actor.</returns>
    /// <exception cref="ArgumentNullException">The request is null.</exception>
    /// <exception cref="InvalidOperationException">The gateway returned an invalid response or the request has no registered opcode.</exception>
    /// <exception cref="RpcException">The RPC timed out or returned an error that requires an exception.</exception>
    public static async ETTask<IResponse> Call(
        this MessageSender self,
        ActorAddress actorAddress,
        IRequest request,
        long timeOutMs = MessageSender.TimeOutMS,
        bool needException = true
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        
        IResponse response;
        if(actorAddress.Process == self.Root().Process)
        {
            //同进程消息
            response =  await self.RootScene().GetComponent<ProcessInnerMessageProxy>().Call(actorAddress, request, needException);
        }
        else
        {
            InternalMessageCallEnvelope envelope = new()
            {
                OriginRoot = self.Root().RootAddress,
                TargetAddress = actorAddress,
                Payload = MessageSerializer.SerializePayload((MessageObject)request, out ushort opcode),
                PayloadOpcode = opcode,
            };
            LogMsg.Instance.DebugInternalGateway(envelope.OriginRoot, envelope.TargetAddress,
                envelope.PayloadOpcode, InternalMessageGatewayLogStages.SEND_REQUEST, request.RpcId);
            IResponse gatewayResponse = await self.RootScene().GetComponent<ProcessInnerMessageProxy>()
                .Call(
                new ActorAddress(new RootAddress(self.Root().Process, InternalMessageGateway.ROOT_ID)),
                envelope, timeOutMs, needException: false);

            if (gatewayResponse is not InternalMessageCallResponse responseEnvelope)
            {
                throw new InvalidOperationException($"Unexpected internal gateway response: {gatewayResponse.GetType().FullName}.");
            }

            request.RpcId = responseEnvelope.RpcId;
            if (responseEnvelope.Payload == null || responseEnvelope.Payload.Length == 0)
            {
                response = MessageOpcodeManager.Instance.CreateResponseBy(request.GetType(), request.RpcId,
                    responseEnvelope.Error);
                response.Message = responseEnvelope.Message;
            }
            else
            {
                response = MessageSerializer.DeserializePayload(responseEnvelope.PayloadOpcode, responseEnvelope.Payload) as IResponse
                    ?? throw new InvalidOperationException("The internal gateway payload is not an RPC response.");
            }
        }

        if (response.Error == ErrorCode.ERR_TASK_TIMEOUT)
        {
            throw new RpcException(response.Error, $"Rpc error: request, 注意Actor消息超时，请注意查看是否死锁或者没有reply: actorId: {actorAddress} {request}, response: {response}");
        }
        if (needException && ErrorCode.IsRpcNeedThrowException(response.Error))
        {
            throw new RpcException(response.Error, $"Rpc error: actorId: {actorAddress} {request}, response: {response}");
        }
        
        return response;
    }

    /// <summary>Gets or creates the internal Session to a process from this Root's Main network component.</summary>
    /// <param name="self">The sender bound to the Main Root.</param>
    /// <param name="process">The destination process identifier.</param>
    /// <returns>The reusable internal Session.</returns>
    public static Session GetSession(this MessageSender self, int process)
    {
        if (!self.DicProcessId2SessionRef.TryGetValue(process, out var sessionRef))
        {
            var networkComponent = self.RootScene().GetComponent<NetworkComponent>();
            var processCfg = ConfigManager.Instance.Tables.TbServerProcess.Get(process);

            var session = networkComponent.CreateConnection(processCfg.InnerIpInnerPort);
            session.IsInternal = true;
            self.DicProcessId2SessionRef.Add(process, session);
            sessionRef = session;
        }
        return (Session)sessionRef;
    }
}