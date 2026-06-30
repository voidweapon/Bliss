namespace Bliss
{
    public class ClientSessionComponent : Entity, IAwake<Session>, IDestroy
    {
        public EntityRef<Session> Session;
    }
}