using System.Numerics;
using Unity.Mathematics;

namespace Bliss
{
    public interface IGameplayPhysicsHandler
    {
        int RaycastNonAlloc(LogicScene scene, float3 origin, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results);
        bool SphereCast(LogicScene scene, float3 origin, float radius, float3 direction, float maxDistance, int layerMask, out GameplayRaycastHit result);
        int SphereCastAllNonAlloc(LogicScene scene, float3 origin, float radius, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results);
        
        int CapsuleCastNonAlloc(LogicScene scene, float3 point0, float3 point1, float radius, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results);
        
        int OverlapCapsuleNonAlloc(LogicScene scene, float3 point0, float3 point1, float radius, int layerMask, GameplayCollider[] results);
        
        bool ComputePenetration(LogicScene scene, GameplayCollider colliderA, float3 positionA, quaternion rotationA, GameplayCollider colliderB, float3 positionB, quaternion rotationB, out float3 direction, out float distance);
        
        bool GetIgnoreLayerCollision(LogicScene scene, int layer1, int layer2);
    }
    
    
    public static class GameplayPhysics
    {
        public static IGameplayPhysicsHandler GameplayPhysicsHandler;

        public static int RaycastNonAlloc(LogicScene scene, float3 origin, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results)
        {
            return GameplayPhysicsHandler.RaycastNonAlloc(scene, origin, direction, maxDistance, layerMask, results);
        }
        
        public static bool SphereCast(LogicScene scene, float3 origin, float radius, float3 direction, float maxDistance, int layerMask, out GameplayRaycastHit result)
        {
            return GameplayPhysicsHandler.SphereCast(scene, origin, radius, direction, maxDistance, layerMask, out result);
        }

        public static int SphereCastAllNonAlloc(LogicScene scene, float3 origin, float radius, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results)
        {
            return GameplayPhysicsHandler.SphereCastAllNonAlloc(scene, origin, radius, direction, maxDistance, layerMask, results);
        }

        public static int CapsuleCastNonAlloc(LogicScene scene, float3 point0, float3 point1, float radius, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results)
        {
            return GameplayPhysicsHandler.CapsuleCastNonAlloc(scene, point0, point1, radius, direction, maxDistance, layerMask, results);
        }
        
        public static int OverlapCapsuleNonAlloc(LogicScene scene, float3 point0, float3 point1, float radius, int layerMask, GameplayCollider[] results)
        {
            return GameplayPhysicsHandler.OverlapCapsuleNonAlloc(scene, point0, point1, radius, layerMask, results);
        }
        
        public static bool ComputePenetration(LogicScene scene, GameplayCollider colliderA, float3 positionA, quaternion rotationA, GameplayCollider colliderB, float3 positionB, quaternion rotationB, out float3 direction, out float distance)
        {
            return GameplayPhysicsHandler.ComputePenetration(scene, colliderA, positionA, rotationA, colliderB, positionB, rotationB, out direction, out distance);
        }
        
        public static bool GetIgnoreLayerCollision(LogicScene scene,  int layer1, int layer2)
        {
            return GameplayPhysicsHandler.GetIgnoreLayerCollision(scene, layer1, layer2);
        }
    }
}