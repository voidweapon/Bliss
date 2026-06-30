using Bliss.Mathematics;

namespace Bliss
{
    
    public static partial class PlayerSpawnPointSystems
    {
        public static transform GetSpawnTransform(this PlayerSpawnPoint self)
        {
            return new transform(self.SceneActorCmpConfig.Position,  self.SceneActorCmpConfig.Rotation);
        }
        
    }
}