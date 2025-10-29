using Unity.Mathematics;

namespace Bliss.Mathematics
{
    public static partial class mathEx
    {
        public static float3 RotateVector(this quaternion self, float3 vector)
        {
            // http://people.csail.mit.edu/bkph/articles/Quaternions.pdf
            // V' = V + 2w(Q x V) + (2Q x (Q x V))
            // refactor:
            // V' = V + w(2(Q x V)) + (Q x (2(Q x V)))
            // T = 2(Q x V);
            // V' = V + w*(T) + (Q x T)
            
            float3 Q = new float3(self.value.x, self.value.y, self.value.z);
            float3 TT = 2f * math.cross(Q, vector);
            float3 Result = vector + (self.value.w * TT) + math.cross(Q, TT);

            return Result;
        }

        public static float3 UnrotateVector(this quaternion self, float3 vector)
        {
            float3 Q = new float3(-self.value.x, -self.value.y, -self.value.z);
            float3 TT = 2f * math.cross(Q, vector);
            float3 Result = vector + (self.value.w * TT) + math.cross(Q, TT);

            return Result;
        }

        public static bool Equals(this quaternion self, quaternion other,float tolerance)
        {
            return (math.abs(self.value.x - other.value.x) <= tolerance && math.abs(self.value.y - other.value.y) <= tolerance && math.abs(self.value.z - other.value.z) <= tolerance && math.abs(self.value.w - other.value.w) <= tolerance)
                   || (math.abs(self.value.x + other.value.x) <= tolerance && math.abs(self.value.y + other.value.y) <= tolerance && math.abs(self.value.z + other.value.z) <= tolerance && math.abs(self.value.w + other.value.w) <= tolerance);
        }
        
    }
}