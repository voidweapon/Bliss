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

        public static quaternion FastLerp(quaternion a, quaternion b, float t)
        {
            float dotResult = math.dot(a, b);
            float bias = math.select(-1f, 1f, dotResult > 0);
            return math.normalizesafe(Add(Scale(b, t), Scale(a, bias * (1f - t))));
        }

        public static quaternion Scale(quaternion a, float scale)
        {
            return new quaternion(a.value * scale);
        }
        
        public static quaternion Add(quaternion a, quaternion b)
        {
            return new quaternion(a.value + b.value);
        }
        
        
        public static bool Equals(this quaternion self, quaternion other,float tolerance)
        {
            return (math.abs(self.value.x - other.value.x) <= tolerance && math.abs(self.value.y - other.value.y) <= tolerance && math.abs(self.value.z - other.value.z) <= tolerance && math.abs(self.value.w - other.value.w) <= tolerance)
                   || (math.abs(self.value.x + other.value.x) <= tolerance && math.abs(self.value.y + other.value.y) <= tolerance && math.abs(self.value.z + other.value.z) <= tolerance && math.abs(self.value.w + other.value.w) <= tolerance);
        }

        public static bool IsNaN(this quaternion self)
        {
            if (float.IsNaN(self.value.x) || float.IsNaN(self.value.y) || float.IsNaN(self.value.z) || float.IsNaN(self.value.w))
            {
                return true;
            }
            return false;
        }
        
    }
}