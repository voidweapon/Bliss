using MemoryPack;
using Unity.Mathematics;

namespace Bliss
{
    public class MovablePlatform : ASceneActorCmp<MovablePlatform, MovablePlatformConfig>, IAfterSceneInit, IDestroy
    {
        public bool MoveForward;
        public float3 MoveDirection;
    }
    
    [System.Serializable]
    [MemoryPackable]
    public partial class MovablePlatformConfig : ASceneActorConfig
    {
        public float3 Point1;
        public float3 Point2;
        public bool InitialDirection;
        public float Speed;
    }
}