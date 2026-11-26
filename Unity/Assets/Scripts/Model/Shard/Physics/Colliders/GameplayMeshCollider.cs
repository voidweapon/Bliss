using System.Runtime.InteropServices;
using Unity.Mathematics;

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
    
    public static class MeshColliderInitParamsHelper
    {
        public static GameplayMeshColliderInitParams ToParam(this MeshColliderConfig config)
        {
            return new GameplayMeshColliderInitParams()
            {
                Convex = config.IsConvex,
                IsTrigger = false
            };
        }
    }
}