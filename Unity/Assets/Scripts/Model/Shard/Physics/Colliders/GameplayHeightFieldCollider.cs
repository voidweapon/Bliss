using System.Runtime.InteropServices;

namespace Bliss
{
    public  partial class GameplayHeightFieldCollider : GameplayCollider, IAwake<GameplayHeightFieldColliderInitParams>, IDestroy
    {
    
    }

    [StructLayout(LayoutKind.Auto)]
    public partial struct GameplayHeightFieldColliderInitParams
    {
        public bool EnableTreeCollider;
    }
    
    public struct GameplayHeightFieldColliderAwakeParams
    {
        public GameplayHeightFieldCollider Collider;

    }

    public struct GameplayHeightFieldDestroyParams
    {
        public GameplayHeightFieldCollider Collider;
    }
}