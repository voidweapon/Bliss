using Bliss.Core;
using Bliss.Timer;
using ET;

namespace Bliss;


[EntitySystemOf(typeof(ActorNetworkLocationLock))]
public static partial class ActorNetworkLocationLockSystems
{
    private static void Destroy(this ActorNetworkLocationLock self)
    {
        self.ActorLocation = default;
        self.CoroutineLock.Dispose();
    }
}

public static class ActorNetworkLocationGroupSystems
{
    /// <summary>
    /// 添加ActorLocation, Actor首次注册到实体网络位置管理处时调用。添加后获取ActorLocation会返回address
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entityId"></param>
    /// <param name="address"></param>
    public static async ETTask Add(this ActorNetworkLocationGroup self, long entityId, ActorAddress address)
    {
        int coroutineLockType = ((int)self.Id << 16) | ECoroutineLockType.Location;
        using (await self.RootScene().GetComponent<CoroutineLockComponent>().WaitAsync(coroutineLockType, entityId))
        {
            self.Locations[entityId] = address;
            Log.Info($"location add EntityId: {entityId} Address: {address}");
        }
    }
    
    /// <summary>
    /// 移除ActorLocation, Actor从网络中移除时调用。移除后获取ActorLocation会返回null
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entityId"></param>
    public static async ETTask Remove(this ActorNetworkLocationGroup self, long entityId)
    {
        int coroutineLockType = ((int)self.Id << 16) | ECoroutineLockType.Location;
        using (await self.RootScene().GetComponent<CoroutineLockComponent>().WaitAsync(coroutineLockType, entityId))
        {
            self.Locations.Remove(entityId);
            Log.Info($"location remove EntityId: {entityId}");
        }
    }

    /// <summary>
    /// 锁定ActorLocation，防止在切换过程中被访问到。锁定后会有一个超时自动解锁，防止死锁
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entityId"></param>
    /// <param name="address"></param>
    /// <param name="timeOutMS"></param>
    public static async ETTask Lock(this ActorNetworkLocationGroup self, long entityId, ActorAddress address, long timeOutMS)
    {
        int coroutineLockType = ((int)self.Id << 16) | ECoroutineLockType.Location;
        var coroutineLock = await self.RootScene().GetComponent<CoroutineLockComponent>().WaitAsync(coroutineLockType, entityId);
        
        var locationLock = self.AddChild<ActorNetworkLocationLock>();
        locationLock.ActorLocation = address;
        locationLock.CoroutineLock = coroutineLock;
        
        self.ActorLocationLocks.Add(entityId, locationLock);
        
        Log.Info($"location lock EntityId: {entityId} Address: {address}");

        if (timeOutMS > 0)
        {
            async ETTask TimeWaitAsync()
            {
                long lockInstanceId = locationLock.InstanceId;
                await self.RootScene().GetComponent<TimerComponent>().WaitDurationAsync(timeOutMS);
                if (locationLock.InstanceId != lockInstanceId)
                {
                    return;
                }
                self.UnLock(entityId, address, address, 1);
            }
            TimeWaitAsync().Coroutine();
        }
    }
    
    /// <summary>
    /// 解锁ActorLocation，切换完成后调用。解锁后会更新ActorLocation为newAddress
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entityId"></param>
    /// <param name="oldAddress"></param>
    /// <param name="newAddress"></param>
    /// <param name="reason"></param>
    public static void UnLock(this ActorNetworkLocationGroup self, long entityId, ActorAddress oldAddress,  ActorAddress newAddress, int reason = 0)
    {
        if (!self.ActorLocationLocks.TryGetValue(entityId, out EntityRef<ActorNetworkLocationLock> lockInfoRef))
        {
            Log.Error($"location unlock not found EntityId: {entityId} {oldAddress}");
            return;
        }

        ActorNetworkLocationLock lockInfo = (ActorNetworkLocationLock)lockInfoRef;
        if (oldAddress != lockInfo.ActorLocation)
        {
            Log.Error($"location unlock oldInstanceId is different: {entityId} {oldAddress}");
            return;
        }

        if (reason == 1)
        {
            Log.Info($"location timeout unlock EntityId: {entityId} oldAddress: {oldAddress} newAddress: {newAddress}");
        }
        Log.Info($"location unlock EntityId: {entityId} oldAddress: {oldAddress} newAddress: {newAddress}");
        
        self.Locations[entityId] = newAddress;

        self.ActorLocationLocks.Remove(entityId);

        // 解锁
        lockInfo.Dispose();
    }
    
    /// <summary>
    /// 获取ActorLocation，获取时会锁定ActorLocation，防止在切换过程中被访问到
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entityId"></param>
    /// <returns></returns>
    public static async ETTask<ActorAddress> Get(this ActorNetworkLocationGroup self, long entityId)
    {
        int coroutineLockType = ((int)self.Id << 16) | ECoroutineLockType.Location;
        using (await self.RootScene().GetComponent<CoroutineLockComponent>().WaitAsync(coroutineLockType, entityId))
        {
            self.Locations.TryGetValue(entityId, out ActorAddress address);
            Log.Info($"location get EntityId: {entityId} ActorAddress: {address}");
            return address;
        }
    }
}

public static class ActorNetworkLocationManagerSystems
{
    /// <summary>
    /// 获取或添加ActorNetworkLocationGroup，不同的ActorCategory会分配到不同的ActorNetworkLocationGroup中
    /// </summary>
    /// <param name="self"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    public static ActorNetworkLocationGroup GetOrAdd(this ActorNetworkLocationManager self, ENetworkLocationCategory category)
    {
        long id = (long)category;
        var locationGroup = self.GetChild<ActorNetworkLocationGroup>(id);
        if (locationGroup == null)
        {
            locationGroup = self.AddChildWithId<ActorNetworkLocationGroup>(id);
        }
        
        return locationGroup;
    }
}