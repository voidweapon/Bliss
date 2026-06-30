using Bliss.Core;
using Unity.Mathematics;

namespace Bliss.Physics
{
    [EntitySystemOf(typeof(PhysicalScene))]
    public static partial class PhysicalSceneSystems
    {
        private static void Awake(this PhysicalScene self)
        {
            EventSystem.Instance.Invoke(PhysicalInvokeTypes.Physical_Scene_Awake, new Invoke_PhysicalSceneAwake());
        }
        private static void Destroy(this PhysicalScene self)
        {
            EventSystem.Instance.Invoke(PhysicalInvokeTypes.Physical_Scene_Destroy, new Invoke_PhysicalSceneDestroy());
        }
        
        public static int SpereCastNonAlloc(this PhysicalScene self, float3 origin, float radius, float3 direction, float maxDistance, int layerMask, GameplayRaycastHit[] results)
        {
            int count = EventSystem.Instance.Invoke<Invoke_SpereCastNonAlloc, int>(PhysicalInvokeTypes.Physical_Scene_Query, new Invoke_SpereCastNonAlloc()
            {
                origin = origin,
                radius = radius,
                direction = direction,
                maxDistance = maxDistance,
                layerMask = layerMask,
                results = results
            });

            return count;
        }

    }
}