using System.Runtime.CompilerServices;
using Bliss.Mathematics;
using Unity.Mathematics;
using MagicPhysX;


namespace Bliss
{
    public static class PxTypeHelper
    {
        private const int CAPSULE_AXIS_X = 0;
        private const int CAPSULE_AXIS_Y = 1;
        private const int CAPSULE_AXIS_Z = 2;
        private const float RIGHT_ANGLE_DEGREES = 90f;
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

        /// <summary>Converts a Unity capsule axis to the PhysX capsule's local rotation.</summary>
        /// <param name="unityCapsuleDirection">Unity axis: zero for X, one for Y, or two for Z.</param>
        /// <returns>The rotation that aligns the PhysX X-axis capsule with the requested axis.</returns>
        public static PxQuat GetPxCapsuleLocalQuat(int unityCapsuleDirection)
        {
            PxQuat rotation = quaternion.identity.ToPxQuat();
            if (unityCapsuleDirection == CAPSULE_AXIS_X)
            {
                rotation = quaternion.identity.ToPxQuat();
            }
            else if (unityCapsuleDirection == CAPSULE_AXIS_Y)
            {
                rotation = quaternion.AxisAngle(mathEx.Forward, math.radians(RIGHT_ANGLE_DEGREES)).ToPxQuat();
            }
            else if (unityCapsuleDirection == CAPSULE_AXIS_Z)
            {
                rotation = quaternion.AxisAngle(mathEx.Up, math.radians(RIGHT_ANGLE_DEGREES)).ToPxQuat();
            }
            
            return rotation;
        }

        /// <summary>Converts a PhysX capsule local rotation back to its Unity axis.</summary>
        /// <param name="pxCapsuleLocalQuat">The capsule local rotation.</param>
        /// <returns>The matching Unity axis, or X when no known rotation matches.</returns>
        public static int GetPxCapsuleLocalQuat(PxQuat pxCapsuleLocalQuat)
        {
            quaternion rotation = pxCapsuleLocalQuat.ToQuaternion();
            if (rotation.Equals(quaternion.identity)) return CAPSULE_AXIS_X;
            if (rotation.Equals(quaternion.AxisAngle(mathEx.Forward, math.radians(RIGHT_ANGLE_DEGREES)))) return CAPSULE_AXIS_Y;
            if (rotation.Equals(quaternion.AxisAngle(mathEx.Up, math.radians(RIGHT_ANGLE_DEGREES)))) return CAPSULE_AXIS_Z;
            return CAPSULE_AXIS_X;
        }
    }
    
}