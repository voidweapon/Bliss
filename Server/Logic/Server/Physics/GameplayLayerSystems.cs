using Bliss.Core;

namespace Bliss.Physics;

[EntitySystemOf(typeof(GameplayLayer))]
public static partial class GameplayLayerSystems
{
    private static void Awake(this GameplayLayer self, int param1)
    {
        self.Layer = param1;
    }
}