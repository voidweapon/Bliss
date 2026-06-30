using ET;

namespace Bliss;

[MessageHandler(ELogicSceneType.Location)]
public class MessageHandler_ObjectAddRequest : MessageHandler<LogicScene, ObjectAddRequest, ObjectAddResponse>
{
    protected override async ETTask Run(LogicScene mapScene, ObjectAddRequest request, ObjectAddResponse response)
    {
        await mapScene.GetComponent<ActorNetworkLocationManager>().GetOrAdd((ENetworkLocationCategory)request.Type).Add(request.Key, request.ActorId);
    }
}

[MessageHandler(ELogicSceneType.Location)]
public class MessageHandler_ObjectRemoveRequest : MessageHandler<LogicScene, ObjectRemoveRequest, ObjectRemoveResponse>
{
    protected override async ETTask Run(LogicScene mapScene, ObjectRemoveRequest request, ObjectRemoveResponse response)
    {
        await mapScene.GetComponent<ActorNetworkLocationManager>().GetOrAdd((ENetworkLocationCategory)request.Type).Remove(request.Key);
    }
}

[MessageHandler(ELogicSceneType.Location)]
public class MessageHandler_ObjectGetRequest : MessageHandler<LogicScene, ObjectGetRequest, ObjectGetResponse>
{
    protected override async ETTask Run(LogicScene mapScene, ObjectGetRequest request, ObjectGetResponse response)
    {
        ActorAddress actorAddress = await mapScene.GetComponent<ActorNetworkLocationManager>().GetOrAdd((ENetworkLocationCategory)request.Type).Get(request.Key);
        response.ActorId = actorAddress;
    }
}

[MessageHandler(ELogicSceneType.Location)]
public class MessageHandler_ObjectLockRequest : MessageHandler<LogicScene, ObjectLockRequest, ObjectLockResponse>
{
    protected override async ETTask Run(LogicScene mapScene, ObjectLockRequest request, ObjectLockResponse response)
    {
        await mapScene.GetComponent<ActorNetworkLocationManager>().GetOrAdd((ENetworkLocationCategory)request.Type).Lock(request.Key, request.ActorId, request.Time);
    }
}

[MessageHandler(ELogicSceneType.Location)]
public class MessageHandler_ObjectUnLockRequest : MessageHandler<LogicScene, ObjectUnLockRequest, ObjectUnLockResponse>
{
    protected override async ETTask Run(LogicScene mapScene, ObjectUnLockRequest request, ObjectUnLockResponse response)
    {
        mapScene.GetComponent<ActorNetworkLocationManager>().GetOrAdd((ENetworkLocationCategory)request.Type).UnLock(request.Key, request.OldActorId, request.NewActorId);
        await ETTask.CompletedTask;
    }
}