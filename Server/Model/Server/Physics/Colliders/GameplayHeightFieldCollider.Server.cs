using MagicPhysX;

namespace Bliss
{
    public unsafe partial class GameplayHeightFieldCollider
    { 
        public PxHeightFieldGeometry* PxHeightFieldGeometrySphereGeometry => (PxHeightFieldGeometry*)GeometryPtr;
    }

    public unsafe partial struct GameplayHeightFieldColliderInitParams
    {
        public PxShape* PxShape;
    }
}