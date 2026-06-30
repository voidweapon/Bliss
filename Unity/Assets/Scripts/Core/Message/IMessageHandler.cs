using System;
using ET;

namespace Bliss
{
    public interface IMessageHandler
    {
        ETTask Handle(Entity entity, RootAddress fromAddress, MessageObject actorMessage);
        Type GetRequestType();
        Type GetResponseType();
    }
}