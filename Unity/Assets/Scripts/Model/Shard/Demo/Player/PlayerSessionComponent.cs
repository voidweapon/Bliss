namespace Bliss
{
    public class PlayerSessionComponent : Entity, IDestroy
    {
        private EntityRef<Session> sessionRef;

        public Session Session
        {
            get => (Session)sessionRef;
            set => sessionRef = value;
        }
    }
}