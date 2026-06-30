using System;
using Bliss.Core.EventSystem;
using ET;

namespace Bliss
{
    /// <summary>
    /// Gate场景外网消息路由
    /// </summary>
    [Invoke((long)ELogicSceneType.Gate)]
    public class InvokeHandler_OnReceiveMessage_Gate : AInvokeHandler<Invoke_OnReceiveMessage>
    {
        public override void Handle(Invoke_OnReceiveMessage args)
        {
            OnReceiveMessage(args).Coroutine();
        }

        public async ETTask OnReceiveMessage(Invoke_OnReceiveMessage args)
        {
            Session session = args.Session;
            object message = args.Message;
            switch (message)
            {
                case ISessionMessage sessionMessage:
                {
                    SessionMessageDispatcher.Instance.Handle(session, sessionMessage);
                    break;
                }
                case ILocationMessage actorLocationMessage:
                {
                    long unitId = session.GetComponent<SessionPlayerComponent>().Player.Id;
                    session.RootScene().GetComponent<MessageLocationSenderManager>().Get((int)ENetworkLocationCategory.Unit).Send(unitId, actorLocationMessage);
                    break;
                }
                case ILocationRequest actorLocationRequest:
                {
                    long unitId = session.GetComponent<SessionPlayerComponent>().Player.Id;
                    int rpcId = actorLocationRequest.RpcId;
                    long instanceId = session.InstanceId;
                    IResponse response = await session.RootScene().GetComponent<MessageLocationSenderManager>() .Get((int)ENetworkLocationCategory.Unit).CallAsync(unitId, actorLocationRequest);
                    response.RpcId =  rpcId;
                    if (session.InstanceId == instanceId)
                    {
                        session.Send((MessageObject)response);
                    }
                    break;
                }
                case IResponse response:
                {
                    session.OnResponse(response);
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