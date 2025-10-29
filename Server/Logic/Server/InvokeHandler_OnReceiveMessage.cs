using System;
using Bliss.Core.EventSystem;

namespace Bliss
{
    [Invoke((long)ELogicSceneType.Main)]
    public class InvokeHandler_OnReceiveMessage : AInvokeHandlerHandler<Invoke_OnReceiveMessage>
    {
        public override void Handle(Invoke_OnReceiveMessage args)
        {
            Session session = args.Session;
            object message = args.Message;
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