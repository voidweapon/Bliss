using Unity.Mathematics;

namespace Bliss.Physics
{
    public class PhysicalScene :  Entity, IAwake, IDestroy
    {
        
    }

    public struct Invoke_PhysicalSceneAwake
    {
        
    }
    
    public struct Invoke_PhysicalSceneDestroy
    {
        
    }
    
    public struct Invoke_SpereCastNonAlloc
    {
        public float3 origin;
        public float radius;
        public float3 direction;
        public float maxDistance;
        public int layerMask;
        public GameplayRaycastHit[] results;
    }
}