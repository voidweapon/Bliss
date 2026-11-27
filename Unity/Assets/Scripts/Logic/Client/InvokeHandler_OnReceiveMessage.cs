using System;
using Bliss.Core.EventSystem;

namespace Bliss
{
    [Invoke((long)ELogicSceneType.Client)]
    public class InvokeHandler_OnReceiveMessage : AInvokeHandler<Invoke_OnReceiveMessage>
    {
        public override void Handle(Invoke_OnReceiveMessage args)
        {
            Session session = args.Session;
            
            object message = args.Message;
            switch (message)
            {
                case S2C_RootNotReady notification:
                {
                    Log.Warning($"RootNotReady: error={notification.Error}, opcode={notification.RejectedOpcode}");
                    break;
                }
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
                case IActorMessage actorMessage:
                {
                    //session.Scene<LogicScene>().GetComponent<LevelNetworkObjectCollectionComponent>().DispatchMessage(actorMessage);
                    
                    var rootScene = session.RootScene();
                    break;
                }
                case IMessage messageObj:
                {
                    var scene = session.Scene<LogicScene>();
                    MessageDispatcher.Instance.HandleMessageAsync(scene, scene.Root.RootAddress, (MessageObject)messageObj).Coroutine();
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