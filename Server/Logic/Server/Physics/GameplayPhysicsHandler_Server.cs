using Bliss.Physics;
using Unity.Mathematics;

namespace Bliss.Physics;

public class GameplayPhysicsHandler_Server : IGameplayPhysicsHandler
{
    public int RaycastNonAlloc(LogicScene scene, float3 origin, float3 direction, float maxDistance, int layerMask,
        GameplayRaycastHit[] results)
    {
        var physicalScene = scene.GetComponent<PhysicalScene>();
        return physicalScene.RaycastAllNonAlloc(origin, direction, maxDistance, layerMask, results);
    }

    public bool SphereCast(LogicScene scene, float3 origin, float radius, float3 direction, float maxDistance, int layerMask, out GameplayRaycastHit result)
    {
        var physicalScene = scene.GetComponent<PhysicalScene>();
        return physicalScene.SpereCast(origin, radius, direction, maxDistance, layerMask, out result);
    }

    public int SphereCastAllNonAlloc(LogicScene scene, float3 origin, float radius, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results)
    {
        var physicalScene = scene.GetComponent<PhysicalScene>();
        return physicalScene.SpereCastNonAlloc(origin, radius, direction, maxDistance, layerMask, results);
    }

    public int CapsuleCastNonAlloc(LogicScene scene, float3 point0, float3 point1, float radius, float3 direction,
        float maxDistance, int layerMask, GameplayRaycastHit[] results)
    {
        var physicalScene = scene.GetComponent<PhysicalScene>();
        return physicalScene.CapsuleCastNonAlloc(point0, point1, radius, direction, maxDistance, layerMask, results);
    }

    public int OverlapCapsuleNonAlloc(LogicScene scene, float3 point0, float3 point1, float radius, int layerMask,
        GameplayCollider[] results)
    {
        var physicalScene = scene.GetComponent<PhysicalScene>();
        return physicalScene.OverlapCapsuleNonAlloc(point0, point1, radius, layerMask, results);
    }

    public bool ComputePenetration(LogicScene scene, GameplayCollider colliderA, float3 positionA, quaternion rotationA,
        GameplayCollider colliderB, float3 positionB, quaternion rotationB, out float3 direction, out float distance)
    {
        var physicalScene = scene.GetComponent<PhysicalScene>();
        return physicalScene.ComputePenetration(colliderA, positionA, rotationA, colliderB, positionB, rotationB, out direction, out distance);
    }

    public bool GetIgnoreLayerCollision(LogicScene scene, int layer1, int layer2)
    {
        var physicalScene = scene.GetComponent<PhysicalScene>();
        return physicalScene.GetIgnoreLayerCollision(layer1, layer2);
    }
}