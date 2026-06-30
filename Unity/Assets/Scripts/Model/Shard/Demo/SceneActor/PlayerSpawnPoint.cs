using MemoryPack;
using Unity.Mathematics;

namespace Bliss
{
    public class PlayerSpawnPoint : ASceneActorCmp<PlayerSpawnPoint, PlayerSpawnPointConfig>, IAfterSceneInit, IDestroy
    {
        
    }
    
    [System.Serializable]
    [MemoryPackable]
    public partial class PlayerSpawnPointConfig : ASceneActorConfig
    {

    }
}