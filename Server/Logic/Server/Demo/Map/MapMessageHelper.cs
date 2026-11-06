using Bliss.Base.Collections;
using Unity.Mathematics;

namespace Bliss;

public static class MapMessageHelper
{
    /// <summary>
    /// 从Map发送Message给Client
    /// <seealso cref="MailboxMessageHandler_GateSession"/>
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="message"></param>
    public static void SendToClient(Entity entity, IMessage message)
    {
        //发给Unit在Gate上的GateSession,然后GateSession转发给Client
        var rootScene = entity.RootScene();
        rootScene.GetComponent<MessageLocationSenderManager>().Get((int)ENetworkLocationCategory.GateSession).SendToStaticActor(entity.Id, message);
    }

    public static void BroadcastAll(LogicScene scene, float3 position, IMessage message)
    {
        var unitMgr = scene.GetLevelGameplayWorld().GetComponent<UnitManager>();
        foreach (var kv in unitMgr.DicEntityId2Unit)
        {
            var unit = (Unit)kv.Value;
            if (unit == null) continue;

            var remoteGhost = unit.GetComponent<RemoteGhost>();
            if (remoteGhost == null) continue;
            SendToClient(unit, message);
        }
    }
    
    /// <summary>
    /// 广播到所有看见自身的观察者
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="entity"></param>
    /// <param name="message"></param>
    public static void Broadcast(LogicScene scene, Entity entity, IMessage message)
    {
        var aoiManagerComponent = scene.GetLevelGameplayWorld().GetComponent<AoiManagerComponent>();
        var aoiEntityCmp = entity.GetComponent<AOIEntityCmp>();
        aoiEntityCmp.GetId(AOIType.NetworkVisibility, out var networkVisibility);
        
        using var tempAoiIdentifierList = ListComponent<long>.Create();
        aoiManagerComponent.GetVisibleObservers(networkVisibility, tempAoiIdentifierList, AOIType.NetworkVisibility);
        foreach (var aoiIdentifier in tempAoiIdentifierList)
        {
            aoiManagerComponent.Entries.TryGetValue(aoiIdentifier, out var aoiEntry);
            if (aoiEntry == null) continue;
            
            Entity observerEntity = (Entity)aoiEntry.EntityRef;
            if(observerEntity == null) continue;
            
            var remoteGhost = observerEntity.GetComponent<RemoteGhost>();
            if (remoteGhost == null) continue;
            
            SendToClient(observerEntity, message);
        }

    }
}