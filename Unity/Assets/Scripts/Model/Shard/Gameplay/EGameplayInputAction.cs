using System;

namespace Bliss
{
    public enum EGameplayInputAction
    {
        Movement = 0,
        Jump,
        Crouch,
        Evade,
        
        Max,
    }
    
    [Flags]
    public enum EInputActionStatus
    {
        None = 0,
        Down = 1 << 0,
        Pressed = 1 << 1,
        Cancel =  1 << 2,
    }

    public struct GameplayInputActionStatus
    {
        public long TimStamp;
        public EGameplayInputAction Action;
        public EInputActionStatus Status;
    }
}