using System;
using ET;

namespace Bliss
{
    public interface IMessageHandler
    {
        ETTask Handle(Entity entity, MessageObject actorMessage);
        Type GetRequestType();
        Type GetResponseType();
    }
}