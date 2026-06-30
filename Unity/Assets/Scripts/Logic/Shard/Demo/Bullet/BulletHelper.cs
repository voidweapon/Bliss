using System.Numerics;
using Unity.Mathematics;

namespace Bliss
{
    public static class BulletHelper
    {
        public static Bullet CreateBullet(LogicScene scene,
            int bulletConfigId, long entityId,
            ENetRole localRole, ENetRole remoteRole,
            float3 position, quaternion rotation,
            GameplayTargetInfo targetInfo,
            long runtime = 0,
            bool isRaiseEvent = true)
        {
            Bullet bullet = scene.AddChildWithId<Bullet, BulletCreateInfo>(entityId,new BulletCreateInfo()
            {
                BulleConfigId = bulletConfigId,
                LocalRole = localRole,
                RemoteRole = remoteRole,
                TargetInfo = targetInfo,
                Runtime = runtime,
            });
            
            var transformComponent = bullet.AddComponent<TransformComponent>();
            transformComponent.SetPositionAndRotation(position, rotation);
            
            bullet.AddComponent<NetRoleComponent, NetRoleComponentInitParam>(new NetRoleComponentInitParam() { LocalNetRole = localRole, RemoteNetRole = remoteRole });
#if Client
            bullet.AddComponent<SmoothTransformComponent, SmoothTransformComponentInitParam>(new SmoothTransformComponentInitParam(ENetworkSmoothingMode.Linear));
#endif
            var moverComponent = bullet.AddComponent<MoverComponent>();
        
            if (isRaiseEvent)
            {
                var unitConfig = bullet.GetConfig();
                EventSystem.Instance.Push(scene.Scene(), new Event_AfterCreateSceneActor()
                {
                    Actor = bullet,
                    ViewAssetPath = unitConfig.ModelPath,
                });
            }

            bullet.Launch(targetInfo);
            
            return bullet;
        }
        
        
        public static Bullet CreateBullet(this DTOBulletInfo bulletInfo, LogicScene scene)
        {
            return CreateBullet(scene, bulletInfo.ConfigId, bulletInfo.BulletId,
                bulletInfo.LocalRole, bulletInfo.RemoteRole,
                bulletInfo.Position, bulletInfo.Rotation,
                bulletInfo.TargetInfo.Create(scene),
                bulletInfo.Runtime);
        }

        public static DTOBulletInfo CreateDTOBulletInfo(this Bullet bullet, bool isFromPool = false)
        {
            var transformComponent = bullet.GetComponent<TransformComponent>();
            var netRoleComponent = bullet.GetComponent<NetRoleComponent>();
            
            var dtoBulletInfo = DTOBulletInfo.Create(isFromPool);
            dtoBulletInfo.BulletId = bullet.Id;
            dtoBulletInfo.ConfigId = bullet.BulleConfigId;
            dtoBulletInfo.Position = transformComponent.Transform.Location;
            dtoBulletInfo.Rotation = transformComponent.Transform.Rotation;
            dtoBulletInfo.LocalRole = netRoleComponent.LocalNetRole;
            dtoBulletInfo.RemoteRole = netRoleComponent.RemoteNetRole;
            dtoBulletInfo.TargetInfo = bullet.TargetInfo.Create();
            dtoBulletInfo.Runtime = bullet.Runtime;
            return dtoBulletInfo;
        }
    }
}