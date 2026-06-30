using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.CharacterControllers;
using Bliss.Core;
using Bliss.Debugger.GameDebugStats;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bliss
{
    public class PlayerInputComponent : Entity, IAwake, IDestroy, GameInputAction.IGameActions, IInputSource
    {
        public GameInputAction InputAction;

        private float3 _moveInputVector;
        private float3 _lookInputVector;
        private bool _onJump = false;
        private bool _onCrouch = false;
        private bool _onEvade = false;
        
        private EInputActionStatus _jumpInputStatus;

        private FixedCircularArray<GameplayInputActionStatus> _inputActionStatusCache = new FixedCircularArray<GameplayInputActionStatus>(64);

        #region GameInputAction.IGameActions
        public void OnMovement(InputAction.CallbackContext context)
        {
            var input = context.ReadValue<Vector2>();
            // Clamp input
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(input.x, 0f, input.y), 1f);
            _moveInputVector =  moveInputVector;
        }

        public void OnRotation(InputAction.CallbackContext context)
        {
            var input = context.ReadValue<Vector2>();
            var rotationInput = new Vector3(input.x, input.y, 0);
            _lookInputVector = rotationInput;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            CheckButton(ref _onJump, context);
            CheckButton(EGameplayInputAction.Jump, context);
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            CheckButton(ref _onCrouch, context);
        }

        public void OnEvade(InputAction.CallbackContext context)
        {
            CheckButton(ref _onEvade, context);

        }

        private static void CheckButton(ref bool buttonState,InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                buttonState = true;
            }
            else if(context.phase == InputActionPhase.Canceled)
            {
                buttonState = false;
            }
        }
        
        private void CheckButton(EGameplayInputAction inputAction ,InputAction.CallbackContext context)
        {
            var buttonState = EInputActionStatus.None;
            if(context.phase == InputActionPhase.Started)
            {
                buttonState = EInputActionStatus.Down;
            }
            else if (context.phase == InputActionPhase.Performed)
            {
                buttonState = EInputActionStatus.Pressed;
                
            }
            else if(context.phase == InputActionPhase.Canceled)
            {
                buttonState =  EInputActionStatus.Cancel;
            }
            
            
            _inputActionStatusCache.Add(new GameplayInputActionStatus()
            {
                TimStamp = TimeInfo.Instance.ClientFrameTime(),
                Action = inputAction,
                Status = buttonState
            }, out _);
        }
        #endregion


        #region IInputSource
        public float3 GetMovementInput()
        {
            return _moveInputVector;
        }

        public float3 GetLookInput()
        {
            return _lookInputVector;
        }
        

        public bool GetActionDown(EGameplayInputAction action)
        {
            switch (action)
            {
                case EGameplayInputAction.Movement:
                    throw new ArgumentException($"Action {action} is not a button input action.");
                case EGameplayInputAction.Jump:
                    return _onJump;
                case EGameplayInputAction.Crouch:
                    return _onCrouch;
                case EGameplayInputAction.Evade:
                    return _onEvade;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        public void  GetActionStatus(List<GameplayInputActionStatus> result)
        {
            for (int i = 0; i < _inputActionStatusCache.Count; i++)
            {
                result.Add(_inputActionStatusCache[i]);
            }
            _inputActionStatusCache.Clear();
        }


        public quaternion GetCameraRotation()
        {
            return this.RootScene().GetComponent<CameraComponent>().CameraRotation;
        }

        #endregion
    }

    [EntitySystemOf(typeof(PlayerInputComponent))]
    public static partial class PlayerInputComponentSystems
    {
        private static void Awake(this PlayerInputComponent self)
        {
            self.InputAction = new GameInputAction();
            self.InputAction.Game.SetCallbacks(self);
            self.InputAction.Game.Enable();
            GameDebugStates.Instance.AddDebugStat<PlayerInputComponent, float3>("PlayerInput", self, "_lookInputVector", new float4(0,0,0,1));
        }
        private static void Destroy(this PlayerInputComponent self)
        {
            if (self.InputAction != null)
            {
                self.InputAction.Game.Disable();
                self.InputAction.Game.RemoveCallbacks(self);
                self.InputAction.Dispose();
            }
            self.InputAction = null;
            GameDebugStates.Instance.RemoveDebugStat<PlayerInputComponent>("PlayerInput", self, "_lookInputVector");
            
        }
    }
}