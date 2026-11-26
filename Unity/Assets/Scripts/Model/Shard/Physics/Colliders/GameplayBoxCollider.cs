using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Bliss
{
    public partial class GameplayBoxCollider : GameplayCollider, IAwake<BoxColliderInitParams>, IDestroy
    {
        public float3 Center
        {
            get => _center;
            set
            {
                _center = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.BoxColliderSetCenter, new SetBoxColliderCenterParams()
                {
                    Collider = this,
                    Center = value,
                });
            }
        }

        public float3 Size
        {
            get => _size;
            set
            {
                _size = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.BoxColliderSetSize, new SetBoxColliderSizeParams()
                {
                    Collider = this,
                    Size = value,
                });
            }
        }

        private float3 _center;
        private float3 _size;
    }
    
    [StructLayout(LayoutKind.Auto)]
    public partial struct BoxColliderInitParams
    {
        public float3 Center;
        public float3 Size;
        public bool IsTrigger;
    }
    
    public struct GameplayBoxColliderAwakeParams
    {
        public GameplayBoxCollider Collider;
        public BoxColliderInitParams InitParams;
    }
    
    public struct GameplayBoxColliderDestroyParams
    {
        public GameplayBoxCollider Collider;
    }
    
    public struct SetBoxColliderCenterParams
    {
        public GameplayBoxCollider Collider;
        public float3 Center;
    }
    
    public struct SetBoxColliderSizeParams
    {
        public GameplayBoxCollider Collider;
        public float3 Size;
    }

    public static class BoxColliderInitParamsHelper
    {
        public static BoxColliderInitParams ToParam(this BoxColliderConfig config, float3 center)
        {
            return new BoxColliderInitParams()
            {
                Center = center,
                Size = config.HalfExtents * 2,
                IsTrigger = false
            };
        }
    }
}