using System;

namespace Bliss
{
    public interface ISessionMessageHandler
    {
        void Handle(Session session, object message);
        
        Type GetMessageType();

        Type GetResponseType();
    }
}