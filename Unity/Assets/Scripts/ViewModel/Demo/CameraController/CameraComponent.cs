using Bliss.CharacterMovement;
using Bliss.Debugger.GameDebugStats;
using ET;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;

namespace Bliss
{
    public class CameraComponent : Entity, IAwake, IDestroy, ILateUpdate, AxisState.IInputAxisProvider
    {
        public Camera MainCamera;
        public quaternion CameraRotation => MainCamera.transform.rotation;
        
        
        public CinemachineVirtualCamera PlayerVirtualCamera;

        public EntityRef<Entity> TargetRef;
        public ETCancellationToken SetupTargetCancellationToken;
        
        public float GetAxisValue(int axis)
        {
            Entity Target = (Entity)TargetRef;
            if (Target == null) return 0f;
            
            var characterMovementCmp = Target.GetComponent<CharacterMovementComponent>();
            if(characterMovementCmp == null) return 0f;

            GameDebugStates.Instance.SetDebugStat("PlayerInput", nameof(characterMovementCmp.RuntimeDataRef.IntentLook), characterMovementCmp.RuntimeDataRef.IntentLook, new float4(0, 0, 0, 1));
            
            if (axis == 0)
            {
                return characterMovementCmp.RuntimeDataRef.IntentLook.x;
            }

            if (axis == 1)
            {
                return characterMovementCmp.RuntimeDataRef.IntentLook.y;
            }
            
            return 0f;
        }
    }
}