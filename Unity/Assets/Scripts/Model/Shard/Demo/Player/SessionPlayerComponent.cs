namespace Bliss
{
    public class SessionPlayerComponent : Entity, IAwake, IDestroy
    {
        public int DestroyReason;
        
        private EntityRef<Player> playerRef;
        public Player Player
        {
            get => (Player)playerRef;
            set => playerRef = value;
        }
    }
}