using System;
using Bliss.Core.EventSystem;

namespace Bliss
{
    /// <summary>
    /// Realm场景外网消息路由
    /// </summary>
    [Invoke((long)ELogicSceneType.Realm)]
    public class InvokeHandler_OnReceiveMessage_Realm : AInvokeHandler<Invoke_OnReceiveMessage>
    {
        public override void Handle(Invoke_OnReceiveMessage args)
        {
            Session session = args.Session;
            object message = args.Message;
            if (ExternalMessageAdmission.RejectWhileInitializing(session, message, args.ReceivedBeforeRootReady,
                    args.InitializationMessagePolicy))
            {
                return;
            }
            switch (message)
            {
                case IResponse response:
                {
                    session.OnResponse(response);
                    break;
                }
                case ISessionMessage sessionMessage:
                {
                    SessionMessageDispatcher.Instance.Handle(session, sessionMessage);
                    break;
                }
                default:
                {
                    throw new Exception($"not found handler: {message}");
                }
            }
        }
    }
}