using Unity.Mathematics;

namespace Bliss.Mathematics
{
    public static partial class mathEx
    {
        public const float DELTA = 0.00001f;
        public const float KINDA_SMALL_NUMBER = 1e-4f;
        
        public static bool IsNearlyZero(this float3 self, float tolerance = float.Epsilon)
        {
            return math.abs(self.x) <= tolerance && math.abs(self.y) <= tolerance && math.abs(self.z) <= tolerance;
        }
        
        public static bool IsNearlyZero(this float self, float tolerance = float.Epsilon)
        {
            return math.abs(self) <= tolerance;
        }
    }
}