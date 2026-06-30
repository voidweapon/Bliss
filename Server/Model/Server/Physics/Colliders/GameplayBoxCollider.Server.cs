using MagicPhysX;
using Unity.Mathematics;

namespace Bliss
{
    public unsafe partial class GameplayBoxCollider
    {
        public PxBoxGeometry* BoxGeometry => (PxBoxGeometry*)GeometryPtr;
    }
    
    public unsafe partial struct BoxColliderInitParams
    {
        public PxShape* PxShape;
    }
}