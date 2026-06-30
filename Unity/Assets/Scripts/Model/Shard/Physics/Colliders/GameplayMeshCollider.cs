using System.Runtime.InteropServices;

namespace Bliss
{
    public partial class GameplayMeshCollider : GameplayCollider, IAwake<GameplayMeshColliderInitParams>, IDestroy
    {
        public bool Convex;
    }

    [StructLayout(LayoutKind.Auto)]
    public partial struct GameplayMeshColliderInitParams
    {
        public bool Convex;
        public bool IsTrigger;
    }
    
    public struct GameplayMeshColliderAwakeParams
    {
        public GameplayMeshCollider Collider;

    }

    public struct GameplayMeshColliderDestroyParams
    {
        public GameplayMeshCollider Collider;
    }
    
}