using System;
using ET;

namespace Bliss;

public static class ActorNetworkLocationProxyComponentSystems
{
    public static async ETTask Add(this ActorNetworkLocationProxyComponent self, int type, long entityId, ActorAddress actorAddress)
    {
        CheckParam(type, entityId);
        
        Log.Info($"location proxy add {type}, {entityId} {TimeInfo.Instance.ServerNow()}");
        
        ObjectAddRequest request = ObjectAddRequest.Create();
        request.Type = type;
        request.Key = entityId;
        request.ActorId = actorAddress;
        await self.RootScene().GetComponent<MessageSender>().Call(GetLocationSceneAddress(), request);
    }
    
    public static async ETTask Remove(this ActorNetworkLocationProxyComponent self, int type, long entityId)
    {
        CheckParam(type, entityId);
        
        Log.Info($"location proxy remove {type}, {entityId} {TimeInfo.Instance.ServerNow()}");
        
        ObjectRemoveRequest objectRemoveRequest = ObjectRemoveRequest.Create();
        objectRemoveRequest.Type = type;
        objectRemoveRequest.Key = entityId;
        await self.RootScene().GetComponent<MessageSender>().Call(GetLocationSceneAddress(), objectRemoveRequest);
    }

    public static async ETTask<ActorAddress> Get(this ActorNetworkLocationProxyComponent self, int type, long entityId)
    {
        CheckParam(type, entityId);
        
        ObjectGetRequest objectGetRequest = ObjectGetRequest.Create();
        objectGetRequest.Type = type;
        objectGetRequest.Key = entityId;

        ObjectGetResponse response = (ObjectGetResponse)await self.RootScene().GetComponent<MessageSender>() .Call(GetLocationSceneAddress(), objectGetRequest);
        return response.ActorId;
    }

    public static async ETTask Lock(this ActorNetworkLocationProxyComponent self, int type, long entityId, ActorAddress actorId, int time = 60000)
    {
        CheckParam(type, entityId);
        
        Log.Info($"location proxy lock {type}, {entityId} {actorId} {TimeInfo.Instance.ServerNow()}");
        
        ObjectLockRequest objectLockRequest = ObjectLockRequest.Create();
        objectLockRequest.Type = type;
        objectLockRequest.Key = entityId;
        objectLockRequest.ActorId = actorId;
        objectLockRequest.Time = time;
        
        await self.RootScene().GetComponent<MessageSender>().Call(GetLocationSceneAddress(), objectLockRequest);

    }
    
    public static async ETTask UnLock(this ActorNetworkLocationProxyComponent self, int type, long entityId, ActorAddress oldActorId, ActorAddress newActorId)
    {
        CheckParam(type, entityId);
        
        Log.Info($"location proxy unlock {type}, {entityId} {newActorId} {TimeInfo.Instance.ServerNow()}");
        
        ObjectUnLockRequest objectUnLockRequest = ObjectUnLockRequest.Create();
        objectUnLockRequest.Type = type;
        objectUnLockRequest.Key = entityId;
        objectUnLockRequest.OldActorId = oldActorId;
        objectUnLockRequest.NewActorId = newActorId;
        
        await self.RootScene().GetComponent<MessageSender>().Call(GetLocationSceneAddress(), objectUnLockRequest);

    }

    private static void CheckParam(int type, long entityId)
    {
        if (entityId == 0)
        {
            throw new ArgumentException($"entityId is 0, type: {type}");
        }
    }
    

    public static ActorAddress GetActorAddress(this Entity self)
    {
        return new ActorAddress(self.LogicScene.Root.RootAddress, self.InstanceId);
    }

    public static ActorAddress GetLocationSceneAddress()
    {
        return ConfigManager.Instance.Tables.TbServerScene.LocationScene.ActorAddress;
    }
    
    public static async ETTask AddNetworkLocation(this Entity self, int type)
    {
        await self.RootScene().GetComponent<ActorNetworkLocationProxyComponent>().Add(type, self.Id, self.GetActorAddress());
    }
    
    public static async ETTask RemoveNetworkLocation(this Entity self, int type)
    {
        await self.RootScene().GetComponent<ActorNetworkLocationProxyComponent>().Remove(type, self.Id);
    }
}