using System.Numerics;
using Unity.Mathematics;

namespace Bliss.Mathematics
{
    public static partial class mathEx
    {
        public const float DELTA = 0.00001f;
        public const float KINDA_SMALL_NUMBER = 1e-4f;
        
        public static readonly float3 Up = new float3(0, 1, 0);
        public static readonly float3 Down = new float3(0, -1, 0);
        public static readonly float3 Forward = new float3(0, 0, 1);
        public static readonly float3 Backward = new float3(0, 0, -1);
        public static readonly float3 Right = new float3(1, 0, 0);
        public static readonly float3 Left = new float3(-1, 0, 0);

        public static bool IsNearlyZero(this float3 self, float tolerance = float.Epsilon)
        {
            return math.abs(self.x) <= tolerance && math.abs(self.y) <= tolerance && math.abs(self.z) <= tolerance;
        }
        
        public static bool IsNearlyZero(this float self, float tolerance = float.Epsilon)
        {
            return math.abs(self) <= tolerance;
        }

        public static float angle(float3 from, float3 to)
        {
            float num = (float)math.sqrt((double)math.lengthsq(from) * (double)math.lengthsq(to));
            return (double)num < 1.0000000036274937E-15  ? 0.0f : (float)math.degrees(math.acos((double)math.clamp(math.dot(from, to) / num, -1f, 1f)));
        }

        /// <summary>
        /// Projects a vector onto a plane defined by a normal orthogonal to the plane.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="planeNormal"></param>
        /// <returns></returns>
        public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
        {
            float num1 = math.dot(planeNormal, planeNormal);
            if ((double)num1 < (double)math.EPSILON)
            {
                return vector;
            }
            
            float num2 = math.dot(vector, planeNormal);
            return new float3(vector.x - planeNormal.x * num2 / num1, vector.y - planeNormal.y * num2 / num1, vector.z - planeNormal.z * num2 / num1);
        }

        public static float3 slerp(float3 a, float3 b, float percent)
        {
            // Dot product - the cosine of the angle between 2 vectors.
            float dot = math.dot(a, b);
            
            // Clamp it to be in the range of Acos()
            // This may be unnecessary, but floating point
            // precision can be a fickle mistress.
            //Mathf.Clamp(dot, -1.0f, 1.0f);
            // annotation derHugo: like it stands this is indeed completely unnecessary. 
            // If something it should be
            dot = math.clamp(dot, -1.0f, 1.0f);
            
            // Acos(dot) returns the angle between start and end,
            // And multiplying that by percent returns the angle between
            // start and the final result.
            float theta = math.acos(dot) * percent;
            float3 RelativeVec = b - a * dot;
            RelativeVec = math.normalize(RelativeVec);
            
            // Orthonormal basis
            // The final result.
            return ((a * math.cos(theta)) + (RelativeVec * math.sin(theta)));
        }

        /// <summary>
        /// Returns a copy of vector with its magnitude clamped to maxLength.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static float3 ClampMagnitude(float3 vector, float maxLength)
        {
            float sqrMagnitude = math.lengthsq(vector);
            if ((double) sqrMagnitude <= (double) maxLength * (double) maxLength)
                return vector;
            
            float num1 = (float) math.sqrt((double) sqrMagnitude);
            float num2 = vector.x / num1;
            float num3 = vector.y / num1;
            float num4 = vector.z / num1;
            
            return new float3(num2 * maxLength, num3 * maxLength, num4 * maxLength);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="p1">Line Point1</param>
        /// <param name="p2">Line Point2</param>
        /// <returns></returns>
        public static float3 GetNearestPointInLineSegment(float3 point, float3 p1, float3 p2)
        {
            float3 p1p2 = p2 - p1;
            float sqrLenght = math.lengthsq(p1p2);
            if (sqrLenght == 0.0f)
                return p1;
            
            float t = math.dot(point - p1, p1p2) / sqrLenght;
            if (t < 0f) return p1;
            if (t > 1f) return p2;
            return p1 + t * p1p2;
        }
    }
}