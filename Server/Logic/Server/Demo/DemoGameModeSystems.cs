using Bliss.Base.Collections;
using Bliss.Core;
using Bliss.Physics;
using Unity.Mathematics;

namespace Bliss;

[EntitySystemOf(typeof(DemoGameMode))]
public static partial class DemoGameModeSystems
{
    private static void Awake(this DemoGameMode self)
    {
        // var physicalScene = self.Scene().GetComponent<PhysicalScene>();
        // bool hasHit = physicalScene.SpereCast(new float3(0, 0.6f, 0f), 0.5f, new float3(0, 0, 1), 100f, -1, out var hitInfo);
        // if (hasHit)
        // {
        //     Log.Info($"hit name={hitInfo.name} point={hitInfo.point}, normal={hitInfo.normal}, distance={hitInfo.distance}");
        // }
        //
        // using var results = ArrayComponent<GameplayRaycastHit>.Create(64);
        // int hitCount = physicalScene.SpereCastNonAlloc(new float3(-14.15f, 1.5f, 0f), 0.5f, new float3(0, 0, 1), 100f, 1 << 0, results.ItemArray);
        // if (hitCount > 0)
        // {
        //     for (int i = 0; i < hitCount; i++)
        //     {
        //         Log.Info($"hit  name={results.ItemArray[i].name} position={results.ItemArray[i].point} normal={results.ItemArray[i].normal}, distance={results.ItemArray[i].distance}");
        //     }
        // }
        
    }
}