using Unity.Mathematics;

namespace Bliss
{
    public struct GameplayRaycastHit
    {
        public EntityRef<GameplayCollider> Collider;
        public float distance;
        public float3 point;
        public float3 normal;
        public string name;
    }
}