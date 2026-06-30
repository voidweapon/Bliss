using Unity.Mathematics;
using UnityEngine;

namespace Bliss
{
    public static partial class PhysicsHelper
    {
        private static RaycastHit[] s_raycastHits = new RaycastHit[64];

        public static int SpereCastNonAlloc(float3 origin, float radius, float3 direction, float maxDistance, int layerMask, out RaycastHit[] hits)
        {
            int count = UnityEngine.Physics.SphereCastNonAlloc(origin, radius, direction, s_raycastHits, maxDistance, layerMask);
            hits = s_raycastHits;
            return count;
        }
    }
}