namespace Bliss
{
    public class MoverComponent : Entity, IAwake, IDestroy
    {
        public long UpdateTimer;
        public long PrevUpdateTime;
    }
}