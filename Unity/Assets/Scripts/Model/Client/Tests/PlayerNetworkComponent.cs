namespace Bliss
{
    public class PlayerNetworkComponent : Entity, IAwake<Session>, IDestroy
    {
        public Session Session;
    }
}