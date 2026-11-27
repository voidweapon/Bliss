namespace Bliss;

/// <summary>Applies Server Root readiness to external message ingress.</summary>
public static class ExternalMessageAdmission
{
    /// <summary>Rejects ordinary external messages while their target Root is initializing.</summary>
    /// <param name="session">The receiving session.</param>
    /// <param name="message">The received message.</param>
    /// <param name="receivedBeforeRootReady">Whether the message was received before the Root was ready.</param>
    /// <param name="messagePolicy">The policy captured when the message was received.</param>
    /// <returns>Whether the message was rejected.</returns>
    public static bool RejectWhileInitializing(Session session, object message, bool receivedBeforeRootReady,
        RootInitializationMessagePolicy messagePolicy)
    {
        ERootInitializationState state = session.Root().InitializationState;
        if (!receivedBeforeRootReady && (state == ERootInitializationState.Ready || state == ERootInitializationState.Unmanaged))
        {
            return false;
        }

        if (message is IResponse responseMessage)
        {
            return !session.RpcInfos.TryGetValue(responseMessage.RpcId, out RpcInfo rpc)
                || MessageOpcodeManager.Instance.GetResponseTypeBy(rpc.RequestType) != responseMessage.GetType();
        }

        RootInitializationMessagePolicy policy = receivedBeforeRootReady
            ? messagePolicy
            : session.Root().InitializationMessagePolicy;
        if (state != ERootInitializationState.Failed
            && policy != null && policy.Allows(message.GetType(), ERootMessageIngress.External))
        {
            return false;
        }

        if (message is IRequest request && message is not ILocationMessage)
        {
            using MessageObject response = (MessageObject)MessageOpcodeManager.Instance.CreateResponseBy(
                request.GetType(), request.RpcId, ErrorCode.ERR_ROOT_NOT_READY);
            session.Send(response);
            return true;
        }

        if (!MessageOpcodeManager.Instance.TryGetOpcodeTypeBy(message.GetType(), out ushort opcode))
        {
            throw new System.InvalidOperationException($"Message type {message.GetType().FullName} has no opcode.");
        }

        session.Send(new S2C_RootNotReady
        {
            Error = ErrorCode.ERR_ROOT_NOT_READY,
            RejectedOpcode = opcode,
        });
        return true;
    }
}
