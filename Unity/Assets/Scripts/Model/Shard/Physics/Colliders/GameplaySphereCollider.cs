using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Bliss
{
    public partial class GameplaySphereCollider :  GameplayCollider, IAwake<SphereColliderInitParams>, IDestroy
    {
        public float3 Center
        {
            get => _center;
            set
            {
                _center = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.SphereColliderSetCenter, new SetSphereColliderCenterParams()
                {
                    Collider = this,
                    Center = value,
                });
            }
        }

        public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.SphereColliderSetRadius, new SetSphereColliderRadiusParams()
                {
                    Collider = this,
                    Radius = value,
                });
            }
        }

        private float3 _center;
        private float _radius;
    }
    
    [StructLayout(LayoutKind.Auto)]
    public partial struct SphereColliderInitParams
    {
        public float3 Center;
        public float Radius;
        public bool IsTrigger;
    }

    public struct GameplaySphereColliderAwakeParams
    {
        public GameplaySphereCollider Collider;
        public SphereColliderInitParams InitParams;
    }

    public struct GameplaySphereColliderDestroyParams
    {
        public GameplaySphereCollider Collider;
    }
    
    public struct SetSphereColliderCenterParams
    {
        public GameplaySphereCollider Collider;
        public float3 Center;
    }
    public struct SetSphereColliderRadiusParams
    {
        public GameplaySphereCollider Collider;
        public float Radius;
    }

    public static class SphereColliderInitParamsHelper
    {
        public static SphereColliderInitParams ToParam(this SphereColliderConfig config, float3 center)
        {
            return new SphereColliderInitParams()
            {
                Center = center,
                Radius = config.Radius,
                IsTrigger = false
            };
        }
    }
}