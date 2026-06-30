using MagicPhysX;
using Unity.Mathematics;

namespace Bliss
{
    public unsafe partial class GameplayCapsuleCollider
    {
        public PxCapsuleGeometry* CapsuleGeometry => (PxCapsuleGeometry*)GeometryPtr;
        
    }
    
    public unsafe partial struct CapsuleColliderInitParams
    {
        public PxShape* PxShape;
    }
}