using System.Collections.Generic;
using Unity.Mathematics;

namespace Bliss.CharacterControllers
{
    public interface IInputSource
    {
        float3 GetMovementInput();
        float3 GetLookInput();
        bool GetActionDown(EGameplayInputAction  action);
        void GetActionStatus(List<GameplayInputActionStatus> result);
        
        quaternion GetCameraRotation();
    }
}