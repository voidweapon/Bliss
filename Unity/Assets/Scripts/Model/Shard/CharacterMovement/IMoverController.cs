using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    public interface IMoverController
    {
        /// <summary>
        /// This is called to let you tell the PhysicsMover where it should be right now
        /// </summary>
        void UpdateMovement(out float3 goalPosition, out quaternion goalRotation, float deltaTime);
    }
}