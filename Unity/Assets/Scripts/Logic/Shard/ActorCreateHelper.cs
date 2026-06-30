using System;
using ET;

namespace Bliss
{
    // public static partial class ActorCreateHelper
    // {
    //     public static async ETTask<ASceneActorCmp> CreateSceneActor(LogicScene scene, Type sceneActorType,
    //         ASceneActorConfig config, long id, ENetRole netRole, ENetRole remoteNetRole, bool isReplicate, 
    //         string viewAssetPath = null)
    //     {
    //         ASceneActorCmp sceneActorCmp = (ASceneActorCmp)scene.AddChildWithId(sceneActorType, id, true);
    //         sceneActorCmp.SetConfig(config);
    //         
    //         sceneActorCmp.AddComponent<NetRoleComponent, NetRoleComponentInitParam>(new NetRoleComponentInitParam()
    //         {
    //             LocalNetRole = netRole,
    //             RemoteNetRole = remoteNetRole,
    //         });
    //         var transformComponent = sceneActorCmp.AddComponent<TransformComponent>();
    //         
    //         if (isReplicate)
    //         {
    //             sceneActorCmp.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.UnOrderedMessage);
    //             scene.GetComponent<LevelNetworkObjectCollectionComponent>().AddNetworkObject(sceneActorCmp);
    //         }
    //         
    //         if (!string.IsNullOrEmpty(viewAssetPath))
    //         {
    //             await EventSystem.Instance.PushAsync(scene, new  Event_AfterCreateSceneActor()
    //             {
    //                 Actor = sceneActorCmp,
    //                 ViewAssetPath = viewAssetPath
    //             });
    //         }
    //         
    //         return sceneActorCmp;
    //     }
    // }
    
    public struct Event_AfterCreateSceneActor
    {
        public EntityRef<Entity> Actor;
        public string ViewAssetPath;
        public bool LoadViewModel;
    }
}