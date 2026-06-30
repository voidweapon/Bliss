using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    /// <summary>
    /// represents a saved move on the client that has been sent to the server and might need to be played back. 
    /// </summary>
    public struct SavedMove_Character
    {
        public bool IsForceNoCombine;
            
        public long TimeStampMS;
        public long DeltaTimeMS;
            
        public float3 StartPosition;
        public quaternion StartRotation;
        public float3 StartVelocity;
        public float3 StartAcceleration;
        public float3 StartAngularVelocity;
        public int StartMoveMode;
            
        public float3 EndPosition;
        public quaternion EndRotation;
        public float3 EndVelocity;
        public float3 EndAcceleration;
        public float3 EndAngularVelocity;
        public int EndMoveMode;
    }
}