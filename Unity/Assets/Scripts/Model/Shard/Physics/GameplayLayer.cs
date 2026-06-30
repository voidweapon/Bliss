namespace Bliss
{
    public class GameplayLayer : Entity,IAwake<int>
    {
        public int Layer;
    }

    public struct Event_GameplayLayerSetLayer
    {
        public GameplayLayer GameplayLayer;
        public int Layer;
    }
}