using Unity.Mathematics;

namespace Bliss;

/// <summary>Creates server-authoritative Bullets and publishes their initial state.</summary>
public static class ServerBulletHelper
{
    /// <summary>Creates and broadcasts a server-owned Bullet from a live ShooterGear.</summary>
    /// <param name="levelScene">The LevelScene containing the ShooterGear.</param>
    /// <param name="bulletConfigId">The Bullet configuration identifier.</param>
    /// <param name="entityId">The network identifier assigned to the Bullet.</param>
    /// <param name="localRole">The Bullet's server network role.</param>
    /// <param name="remoteRole">The Bullet's client network role.</param>
    /// <param name="position">The Bullet's starting position.</param>
    /// <param name="rotation">The Bullet's starting rotation.</param>
    /// <param name="targetInfo">The target information used by its Mover.</param>
    /// <param name="shooter">The live ShooterGear that created the Bullet.</param>
    /// <param name="isRaiseEvent">Whether to raise the scene actor creation event.</param>
    /// <returns>The created Bullet.</returns>
    public static Bullet LaunchBullet(LogicScene levelScene,
        int bulletConfigId, long entityId,
        ENetRole localRole, ENetRole remoteRole,
        float3 position, quaternion rotation,
        GameplayTargetInfo targetInfo,
        ShooterGear shooter,
        bool isRaiseEvent = true)
    {
        Bullet bullet = BulletHelper.CreateBullet(levelScene, bulletConfigId, entityId,
            localRole, remoteRole, position, rotation, targetInfo, shooter, isRaiseEvent: isRaiseEvent);

        M2C_CreateBullets message = M2C_CreateBullets.Create();
        message.Bullets.Add(bullet.CreateDTOBulletInfo());
        MapMessageHelper.BroadcastAll(levelScene, position, message);
        return bullet;
    }
}
