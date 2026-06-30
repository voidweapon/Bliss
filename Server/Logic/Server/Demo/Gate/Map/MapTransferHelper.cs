using ET;

namespace Bliss;

public static  class MapTransferHelper
{
    
    public static async ETTask StartTransferUnit(Entity entity, ActorAddress targetSceneAddress, bool isLogin)
    {
        long entityId = entity.Id;
        var rootScene = entity.RootScene();
        var transferRequest = M2M_UnitTransferRequest.Create();
        transferRequest.FromAddress = entity.GetActorAddress();
        transferRequest.RawEntity = MongoHelper.Serialize(entity);
        transferRequest.IsLogin = isLogin;
        
        entity.Dispose();
        await rootScene.GetComponent<ActorNetworkLocationProxyComponent>().Lock((int)ENetworkLocationCategory.Unit, entityId, transferRequest.FromAddress);
        await rootScene.GetComponent<MessageSender>().Call(targetSceneAddress, transferRequest);
    }
    
    public static async ETTask FinishTransferUnit(Entity entity, ActorAddress fromAddress)
    {
        long entityId = entity.Id;
        var rootScene = entity.RootScene();
        
        await rootScene.GetComponent<ActorNetworkLocationProxyComponent>().UnLock((int)ENetworkLocationCategory.Unit, entityId, fromAddress, entity.GetActorAddress());
    }
}