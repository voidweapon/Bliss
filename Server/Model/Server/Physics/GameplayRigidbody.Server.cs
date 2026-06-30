using MagicPhysX;
using Unity.Mathematics;
namespace Bliss
{
    public unsafe partial class GameplayRigidbody
    {
        public PxRigidBody* RigidBodyPtr;
    }

    public unsafe partial struct GameplayRigidbodyInitParams
    {
        public PxRigidBody* RigidBodyPtr;
        
    }
}