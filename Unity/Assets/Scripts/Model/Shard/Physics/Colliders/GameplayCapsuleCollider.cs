using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Bliss
{
    public partial class GameplayCapsuleCollider : GameplayCollider, IAwake<CapsuleColliderInitParams>, IDestroy
    {
        public float3 Center
        {
            get => _center;
            set
            {
                _center = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.CapsuleColliderSetCenter, new SetCapsuleColliderCenterParams()
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
                EventSystem.Instance.Invoke(PhysicsInvokeType.CapsuleColliderSetRadius, new SetCapsuleColliderRadiusParams()
                {
                    Collider = this,
                    Radius = value,
                });
            }
        }

        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.CapsuleColliderSetHeight, new SetCapsuleColliderHeightParams()
                {
                    Collider = this,
                    Height = value,
                });
            }
        }

        /// <summary>
        /// The direction of the capsule.
        /// The value can be 0, 1 or 2 corresponding to the X, Y and Z axes, respectively.
        /// </summary>
        public int Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.CapsuleColliderSetDirection, new SetCapsuleColliderDirectionParams()
                {
                    Collider = this,
                    Direction = value,
                });
            }
        }

        private float3 _center;
        private float _radius;
        private float _height;
        private int _direction;
    }
    
    [StructLayout(LayoutKind.Auto)]
    public partial struct CapsuleColliderInitParams
    {
        public float3 Center;
        public float Radius;
        public float Height;
        public int Direction;
        public bool IsTrigger;
    }
    
    public struct GameplayCapsuleColliderAwakeParams
    {
        public GameplayCapsuleCollider Collider;
        public CapsuleColliderInitParams InitParams;
    }
    
    public struct GameplayCapsuleColliderDestroyParams
    {
        public GameplayCapsuleCollider Collider;
    }
    
    public struct SetCapsuleColliderCenterParams
    {
        public GameplayCapsuleCollider Collider;
        public float3 Center;
    }
    
    public struct SetCapsuleColliderRadiusParams
    {
        public GameplayCapsuleCollider Collider;
        public float Radius;
    }
    
    public struct SetCapsuleColliderHeightParams
    {
        public GameplayCapsuleCollider Collider;
        public float Height;
    }

    public struct SetCapsuleColliderDirectionParams
    {
        public GameplayCapsuleCollider Collider;
        public int Direction;
    }
    
    public static class CapsuleColliderInitParamsHelper
    {
        public static CapsuleColliderInitParams ToParam(this CapsuleColliderConfig config, float3 center)
        {
            return new CapsuleColliderInitParams()
            {
                Center = center,
                Radius = config.Radius,
                Height = config.Height,
                Direction = config.Direction,
                IsTrigger = false
            };
        }
    }
}