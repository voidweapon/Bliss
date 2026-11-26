using Unity.Mathematics;

namespace Bliss.Physics
{
    public static class GameplayPhysicalCmpCreateHelper
    {
        public static void CreatePhysicalComponent(this PhysicalScene self, Entity entity, PhysicalRigidActorConfig config)
        {
            self.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(new GameplayRigidbodyInitParams()
            {
                IsStatic = config.IsStatic,
                IsKinematic = config.IsKinematic,
                UseGravity = config.UseGravity
            });

            foreach (var shapeConfig in config.Shapes)
            {
                if(shapeConfig is BoxColliderConfig boxParams)
                {
                    self.AddComponent<GameplayBoxCollider, BoxColliderInitParams>(boxParams.ToParam(float3.zero));
                }
            }
        }
    }
}