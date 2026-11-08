using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(LevelGameWorldComponent))]
    public static partial class LevelGameWorldComponentSystems
    {
        private static void EarlyUpdate(this LevelGameWorldComponent self)
        {
            self.GameplayWorld.EarlyUpdate();
        }

        private static void Update(this LevelGameWorldComponent self)
        {
            self.GameplayWorld.Update();
        }

        private static void LateUpdate(this LevelGameWorldComponent self)
        {
            self.GameplayWorld.LateUpdate();
        }
    }
}
