using Bliss.Base.Collections;
using Unity.Mathematics;

namespace Bliss.Physics;

public static class GameplayPhysicalCmpCreateHelper
{
    public static void CreatePhysicalComponent(this PhysicalScene self, Entity entity, PhysicalRigidActorConfig config)
    {
        self.AddGameplayRigidbodyComponentToEntity(entity, 0, config.IsStatic, config.IsKinematic, config.UseGravity);

        if (config.Shapes != null)
        {
            foreach (var shapeConfig in config.Shapes)
            {
                if(shapeConfig is BoxColliderConfig boxParams)
                {
                    var boxColliderInitParams = new BoxColliderInitParams()
                    {
                        Size = 2.0f * boxParams.HalfExtents,
                        Center = float3.zero,
                        IsTrigger = boxParams.IsTrigger
                    };
                    
                    self.AddGameplayBoxColliderToEntity(entity, 0, boxParams, true);
                }
            }
        }

        
    }
}