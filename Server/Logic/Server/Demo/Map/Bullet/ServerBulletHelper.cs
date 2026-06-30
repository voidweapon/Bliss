using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Bliss;

public static class ServerBulletHelper
{
    public static Entity LaunchBullet(LogicScene scene,
        int bulletConfigId, long entityId,
        ENetRole localRole, ENetRole remoteRole,
        float3 position, quaternion rotation,
        GameplayTargetInfo targetInfo,
        bool isRaiseEvent = true)
    {
        var bullet = BulletHelper.CreateBullet(scene, bulletConfigId, entityId, 
            localRole, remoteRole, 
            position, rotation,
            targetInfo, 0,
            isRaiseEvent);

        var msg = M2C_CreateBullets.Create();
        msg.Bullets.Add(new DTOBulletInfo()
        {
            BulletId = entityId,
            ConfigId = bulletConfigId,
            Position = position,
            Rotation = rotation,
            LocalRole = localRole, 
            RemoteRole = remoteRole,
            TargetInfo = new DTOGameplayTargetInfo()
            {
                Position = targetInfo.TargetPos,
                Direction = targetInfo.TargetDir,
                EntityId = ((Entity)targetInfo.TargetEntity)?.Id ?? 0,
            },
        });
        MapMessageHelper.Broadcast(scene, position, msg);
        
        return bullet;
    }
}