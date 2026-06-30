using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Bliss
{
    public partial class GameplayRigidbody : Entity, IAwake<GameplayRigidbodyInitParams>, IDestroy
    {
        public bool IsStatic;
        public bool isKinematic;

        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.SetMass, new SetMassParams()
                {
                    Rigidbody = this,
                    Mass = _mass
                });
            }
        }

        public float3 Velocity
        {
            get => _velocity;
            set
            {
                _velocity = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.SetVelocity, new SetVelocityParams()
                {
                    Rigidbody = this,
                    Velocity = _velocity
                });
            }
        }

        public float3 AngularVelocity
        {
            get => _angularVelocity;
            set
            {
                _angularVelocity = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.SetAngularVelocity, new SetAngularVelocityParams()
                {
                    Rigidbody = this,
                    AngularVelocity = _angularVelocity
                });
            }
        }

        public float3 CenterOfMass
        {
            get => _centerOfMass;
            set
            {
                _centerOfMass = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.SetCenterOfMass, new SetCenterOfMassParams()
                {
                    Rigidbody = this,
                    CenterOfMass = _centerOfMass
                });
                
            }
        }


        public float3 Position
        {
            get => _position;
            set
            {
                _position = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.SetPosition, new SetPositionParams()
                {
                    Rigidbody = this,
                    Position = _position
                });
            }
        }


        public quaternion Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                EventSystem.Instance.Invoke(PhysicsInvokeType.SetRotation, new SetRotationParams()
                {
                    Rigidbody = this,
                    Rotation = _rotation
                });
            }
        }
        
        private float3 _centerOfMass;
        private float3 _position;
        private quaternion _rotation;
        private float3 _angularVelocity;
        private float3 _velocity;
        private float _mass = 1f;

        public void WakeUp()
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.WakeUp, new WakeUpParams()
            {
                Rigidbody = this,
            });
        }


        public void AddForceAtPosition(float3 force, float3 position, ForceMode mode = ForceMode.Force)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.AddForceAtPosition, new AddForceAtPositionParams()
            {
                Rigidbody = this,
                Force = force,
                Position = position,
                Mode = mode,
            });
        }


        public void MovePosition(float3 newPosition)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.MovePosition, new MovePositionParams()
            {
                Rigidbody = this,
                NewPosition = newPosition
            });
        }
        

        public void MoveRotation(quaternion newRotation)
        {
            EventSystem.Instance.Invoke(PhysicsInvokeType.MoveRotation, new MoveRotationParams()
            {
                Rigidbody = this,
                NewRotation = newRotation
            });
        }
    }
    
    
    [StructLayout(LayoutKind.Auto)]
    public partial struct GameplayRigidbodyInitParams
    {
        public bool IsStatic;
        public bool IsKinematic;
        public bool UseGravity;
    }
    
    public struct GameplayRigidbodyAwakeParams
    {
        public GameplayRigidbody Rigidbody;
        public bool IsStatic;
        public bool IsKinematic;
        public bool UseGravity;
    }
    
    public struct GameplayRigidbodyDestroyParams
    {
        public GameplayRigidbody Rigidbody;
    }
    
    public struct SetMassParams
    {
        public GameplayRigidbody Rigidbody;
        public float Mass;
    }
    public struct SetVelocityParams
    {
        public GameplayRigidbody Rigidbody;
        public float3 Velocity;
    }
    public struct SetAngularVelocityParams
    {
        public GameplayRigidbody Rigidbody;
        public float3 AngularVelocity;
    }
    public struct SetCenterOfMassParams
    {
        public GameplayRigidbody Rigidbody;
        public float3 CenterOfMass;
    }
    public struct SetPositionParams
    {
        public GameplayRigidbody Rigidbody;
        public float3 Position;
    }
    
    public struct SetRotationParams
    {
        public GameplayRigidbody Rigidbody;
        public quaternion Rotation;
    }
    
    public struct WakeUpParams
    {
        public GameplayRigidbody Rigidbody;
    }
    
    public struct AddForceAtPositionParams
    {
        public GameplayRigidbody Rigidbody;
        public float3 Force;
        public float3 Position;
        public ForceMode Mode;
    }
    
    public struct MovePositionParams
    {
        public GameplayRigidbody Rigidbody;
        public float3 NewPosition;
    }
    
    public struct MoveRotationParams
    {
        public GameplayRigidbody Rigidbody;
        public quaternion NewRotation;
    }
    
    public struct MovePositionAndRotationParams
    {
        public GameplayRigidbody Rigidbody;
        public float3 Position;
        public quaternion Rotation;
    }

}