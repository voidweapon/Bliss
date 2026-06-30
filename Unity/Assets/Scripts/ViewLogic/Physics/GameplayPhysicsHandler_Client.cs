using Bliss.Core.EventSystem;
using Unity.Mathematics;
using UnityEngine;

namespace Bliss.Physics
{
    [Invoke(PhysicalInvokeTypes.Physical_Scene_Awake)]
    public class EventHanlder_PhysicalSceneAwake : AInvokeHandler<Invoke_PhysicalSceneAwake>
    {
        public override void Handle(Invoke_PhysicalSceneAwake param)
        {
            
        }
    }
    
    
    [Invoke(PhysicalInvokeTypes.Physical_Scene_Destroy)]
    public class EventHanlder_PhysicalSceneDestroy : AInvokeHandler<Invoke_PhysicalSceneDestroy>
    {
        public override void Handle(Invoke_PhysicalSceneDestroy param)
        {
            
        }
    }

    public class GameplayPhysicsHandler_Client : IGameplayPhysicsHandler
    {
        private static RaycastHit[] _raycastHit = new RaycastHit[64]; 
        private static Collider[] _colliders = new Collider[64]; 
        public int RaycastNonAlloc(LogicScene scene, float3 origin, float3 direction, float maxDistance, int layerMask,
            GameplayRaycastHit[] results)
        {
            int count = UnityEngine.Physics.RaycastNonAlloc(origin, direction, _raycastHit, maxDistance, layerMask);

            int actualCount = math.min(count, results.Length);
            for (int i = 0; i < actualCount; i++)
            {
                var hit = _raycastHit[i];
                results[i] = ConvertToGameplayRaycastHit(hit);
            }
            return actualCount;
        }

        public bool SphereCast(LogicScene scene, float3 origin, float radius, float3 direction, float maxDistance, int layerMask,
            out GameplayRaycastHit result)
        {
            bool hitSomething = UnityEngine.Physics.SphereCast(origin, radius, direction, out var hit, maxDistance, layerMask);
            result = ConvertToGameplayRaycastHit(hit);
            return hitSomething;
        }

        public int SphereCastAllNonAlloc(LogicScene scene, float3 origin, float radius, float3 direction, float maxDistance,
            int layerMask, GameplayRaycastHit[] results)
        {
            int count = UnityEngine.Physics.SphereCastNonAlloc(origin, radius, direction, _raycastHit, maxDistance, layerMask);
            
            int actualCount = math.min(count, results.Length);
            for (int i = 0; i < actualCount; i++)
            {
                var hit = _raycastHit[i];
                results[i] = ConvertToGameplayRaycastHit(hit);
            }
            return actualCount;
        }

        public int CapsuleCastNonAlloc(LogicScene scene, float3 point0, float3 point1, float radius, float3 direction,
            float maxDistance, int layerMask, GameplayRaycastHit[] results)
        {
            int count = UnityEngine.Physics.CapsuleCastNonAlloc(point0, point1, radius, direction, _raycastHit, maxDistance, layerMask);
            
            int actualCount = math.min(count, results.Length);
            for (int i = 0; i < actualCount; i++)
            {
                var hit = _raycastHit[i];
                results[i] = ConvertToGameplayRaycastHit(hit);
            }
            return actualCount;
        }

        public int OverlapCapsuleNonAlloc(LogicScene scene, float3 point0, float3 point1, float radius, int layerMask,
            GameplayCollider[] results)
        {
            int count = UnityEngine.Physics.OverlapCapsuleNonAlloc(point0, point1, radius, _colliders, layerMask);
            
            int actualCount = math.min(count, results.Length);
            for (int i = 0; i < actualCount; i++)
            {
                var collider = _colliders[i];
                results[i] = ConvertToGameplayCollider(collider);
                if (results[i] == null)
                {
                    Log.Info($"{collider.name}:Not Map");
                    actualCount--;
                }
            }
            return actualCount;
        }

        public bool ComputePenetration(LogicScene scene, GameplayCollider colliderA, float3 positionA, quaternion rotationA,
            GameplayCollider colliderB, float3 positionB, quaternion rotationB, out float3 direction, out float distance)
        {
            Collider unityColliderA = ConvertToGameplayCollider(colliderA);
            Collider unityColliderB = ConvertToGameplayCollider(colliderB);
            
            bool result = UnityEngine.Physics.ComputePenetration(unityColliderA, positionA, rotationA, unityColliderB, positionB, rotationB, out var vec3Direction, out distance);
            direction = vec3Direction;
            
            return result;
        }

        public bool GetIgnoreLayerCollision(LogicScene scene, int layer1, int layer2)
        {
            return UnityEngine.Physics.GetIgnoreLayerCollision(layer1, layer2);
        }
        
        private static GameplayRaycastHit ConvertToGameplayRaycastHit(RaycastHit hit)
        {
            return new GameplayRaycastHit()
            {
                Collider = ViewEntityMapManager.Instance.GetGameplayCollider(hit.collider),
                distance = hit.distance,
                point = hit.point,
                normal = hit.normal,
                name = hit.collider.gameObject.name,
            };
        }

        private static GameplayCollider ConvertToGameplayCollider(Collider collider)
        {
            return ViewEntityMapManager.Instance.GetGameplayCollider(collider);
        }

        private static Collider ConvertToGameplayCollider(GameplayCollider gameplayCollider)
        {
            return ViewEntityMapManager.Instance.GetCollider(gameplayCollider);
        }
    }
}