using System;
using ET;
using Unity.Mathematics;

namespace Bliss
{
    public static class ActorCreateHelper
    {
        public static async ETTask<SceneActor> CreateSceneActor(LogicScene scene, int id, ENetRole netRole, ENetRole remoteNetRole, bool isReplicate, string viewAssetPath = null)
        {
            var actor = scene.AddChildWithId<SceneActor>(id);
            actor.AddComponent<NetRoleComponent, NetRoleComponentInitParam>(new NetRoleComponentInitParam()
            {
                LocalNetRole = netRole,
                RemoteNetRole = remoteNetRole,
            });
            var transformComponent = actor.AddComponent<TransformComponent>();
            actor.AddComponent<PeriodicMotionComponent, PeriodicMotionComponent.InitParam>(
                new PeriodicMotionComponent.InitParam()
                {
                    Distance = 10.33f,
                    Velocity = new float3(1f, 0f, 0f),
                    Mode2Duration = 2f,
                    Mode2Interval = 5f,
                    Mode3IntervalVariance = 1f,
                    StableRotationDuration = 3f,
                    AngularVelocity = new float3(0f, 6f, 0f),
                    ChangeRotationInterval = 7f,
                    ChangeRotationIntervalVariance = 1f,
                });

            transformComponent.SetPosition(new float3(0, 0, -5));
            
            if (isReplicate)
            {
                actor.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.OrderedMessage);
                scene.GetComponent<LevelNetworkObjectCollectionComponent>().AddNetworkObject(actor);
            }
            
            if (!string.IsNullOrEmpty(viewAssetPath))
            {
                await EventSystem.Instance.PushAsync(scene, new  Event_AfterCreateSceneActor()
                {
                    Actor = actor,
                    ViewAssetPath = viewAssetPath
                });
            }

            return actor;
        }
    }

    public struct Event_AfterCreateSceneActor
    {
        public SceneActor Actor;
        public string ViewAssetPath;
    }
}