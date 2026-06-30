using MagicPhysX;

namespace Bliss
{
    public unsafe partial class GameplayCollider
    {
        public PxShape* ShapePtr;
        
        public PxGeometry* GeometryPtr => ShapePtr->GetGeometry();
    }
}