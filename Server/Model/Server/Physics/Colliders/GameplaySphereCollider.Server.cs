using MagicPhysX;
using Unity.Mathematics;

namespace Bliss
{
    public unsafe partial class GameplaySphereCollider
    {
        public PxSphereGeometry* SphereGeometry => (PxSphereGeometry*)GeometryPtr;
    }
    
    public unsafe partial struct SphereColliderInitParams
    {
        public PxShape* PxShape;

    }
}