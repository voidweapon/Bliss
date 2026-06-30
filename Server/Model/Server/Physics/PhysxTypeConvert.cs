using System.Runtime.CompilerServices;
using Bliss.Mathematics;
using Unity.Mathematics;
using MagicPhysX;


namespace Bliss
{
    public static class PxTypeHelper
    {
        public static PxVec2 ToPxVec2(this float2 v) => Unsafe.As<float2, PxVec2>(ref v);

        public static float2 ToFloat2(this PxVec2 v) => Unsafe.As<PxVec2, float2>(ref v);

        public static PxVec3 ToPxVec3(this float3 v) => Unsafe.As<float3, PxVec3>(ref v);

        public static float3 ToFloat3(this PxVec3 v) => Unsafe.As<PxVec3, float3>(ref v);

        public static PxVec4 ToPxVec4(this float4 v) => Unsafe.As<float4, PxVec4>(ref v);

        public static float4 ToFloat4(this PxVec4 v) => Unsafe.As<PxVec4, float4>(ref v);

        public static PxQuat ToPxQuat(this quaternion v) => Unsafe.As<quaternion, PxQuat>(ref v);

        public static quaternion ToQuaternion(this PxQuat v) => Unsafe.As<PxQuat, quaternion>(ref v);
        
        public static PxForceMode ToPxForceMode(this ForceMode v) => Unsafe.As<ForceMode, PxForceMode>(ref v);
        
        public static ForceMode ToForceMode(this PxForceMode v) => Unsafe.As<PxForceMode, ForceMode>(ref v);
        
        
        public static float GetPxCapsuleHalfHeight(float unityCapsuleHeight, float unityCapsuleRadius)
        {
            return (unityCapsuleHeight - 2.0f * unityCapsuleRadius) * 0.5f;
        }
        
        public static float GetUnitCapsuleHeight(float pxCapsuleHalfHeight, float pxCapsuleRadius)
        {
            return 2.0f * (pxCapsuleHalfHeight + pxCapsuleRadius);
        }

        public static PxQuat GetPxCapsuleLocalQuat(int unityCapsuleDirection)
        {
            PxQuat rotation = quaternion.identity.ToPxQuat();
            if (unityCapsuleDirection == 0)
            {
                rotation = quaternion.identity.ToPxQuat();
            }
            else if (unityCapsuleDirection == 1)
            {
                rotation = quaternion.AxisAngle(mathEx.Forward, math.degrees(90f)).ToPxQuat();
            }
            else if (unityCapsuleDirection == 2)
            {
                rotation = quaternion.AxisAngle(mathEx.Up, math.degrees(90f)).ToPxQuat();
            }
            
            return rotation;
        }

        public static int GetPxCapsuleLocalQuat(PxQuat pxCapsuleLocalQuat)
        {
            quaternion rotation = pxCapsuleLocalQuat.ToQuaternion();
            if (rotation.Equals(quaternion.identity)) return 0;
            if (rotation.Equals(quaternion.AxisAngle(mathEx.Forward, math.degrees(90f)))) return 1;
            if (rotation.Equals(quaternion.AxisAngle(mathEx.Up, math.degrees(90f)))) return 2;
            return 0;
        }
    }
    
}