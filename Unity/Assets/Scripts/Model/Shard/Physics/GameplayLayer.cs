namespace Bliss
{
    /// <summary>Stores the collision layer of a GameplayWorld-owned entity.</summary>
    public class GameplayLayer : GamePlayEntity, IAwake<int>
    {
        public int Layer;
    }

    public struct Event_GameplayLayerSetLayer
    {
        public GameplayLayer GameplayLayer;
        public int Layer;
    }
}