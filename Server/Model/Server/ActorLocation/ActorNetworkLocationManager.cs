using System.Collections.Generic;
using Bliss.CoroutineLocks;

namespace Bliss;

public enum ENetworkLocationCategory : int
{
    Player,
    Unit,
    SceneActor,
    GateSession,
}

public class ActorNetworkLocationLock : Entity, IDestroy
{
    public ActorAddress ActorLocation;
    public CoroutineLock CoroutineLock;
}

public class ActorNetworkLocationGroup : Entity
{
    /// <summary>
    /// key: entityId
    /// </summary>
    public Dictionary<long, ActorAddress> Locations = new Dictionary<long, ActorAddress>();
    
    /// <summary>
    /// key: entityId
    /// </summary>
    public Dictionary<long, EntityRef<ActorNetworkLocationLock>> ActorLocationLocks = new Dictionary<long, EntityRef<ActorNetworkLocationLock>>();
}


/// <summary>
/// 实体网络位置管理处
/// <seealso cref="ActorNetworkLocationProxyComponent"/>
/// </summary>
public class ActorNetworkLocationManager : Entity
{
    
}