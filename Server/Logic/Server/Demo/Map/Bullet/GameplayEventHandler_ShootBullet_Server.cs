using System;
using Bliss.Id;
using ET;

namespace Bliss
{
    [Event(ELogicSceneType.Level)]
    public class GameplayEventHandler_ShootBullet_Server : AEvent<LogicScene, GameplayEvent_ShootBullet>
    {
        /// <inheritdoc />
        protected override async ETTask Run(LogicScene levelScene, GameplayEvent_ShootBullet eventInfo)
        {
            Entity shooterHost = (Entity)eventInfo.Shooter;
            ShooterGear shooter = shooterHost?.GetComponent<ShooterGear>()
                ?? throw new InvalidOperationException("A shot event requires a live ShooterGear.");
            ServerBulletHelper.LaunchBullet(levelScene, 
                eventInfo.BulletConfigId, IdGenerator.Instance.GenerateId(), 
                ENetRole.Authority, ENetRole.SimulatedProxy,
                eventInfo.Position, eventInfo.Rotation, eventInfo.TargetInfo, shooter);
            
            await ETTask.CompletedTask;
        }
    }
}