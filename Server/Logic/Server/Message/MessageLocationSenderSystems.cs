using System;
using System.Runtime.CompilerServices;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Pool;
using Bliss.Timer;
using ET;

namespace Bliss;

[EntitySystemOf(typeof(MessageLocationSender))]
public static partial class MessageLocationSenderSystems
{
    private static void Awake(this MessageLocationSender self)
    {
        self.CheckTimerId = self.RootTimerComponent().NewRepeatedTimer(10 * 1000, TimerInvokeType.MessageLocationSenderCheck, self);
    }

    private static void Destroy(this MessageLocationSender self)
    {
        self.RootTimerComponent().CancelTimer(ref self.CheckTimerId);
    }

    public static void Check(this MessageLocationSender self)
    {
        
    }

    public static ActorLocationCache GetOrCreateLocationCache(this MessageLocationSender self, long id)
    {
        if (id == 0)
        {
            throw new ArgumentException($"invalid id: {id}");
        }

        var cache = self.GetChild<ActorLocationCache>(id);
        if (cache == null)
        {
            cache = self.AddChildWithId<ActorLocationCache>(id);
        }

        return cache;
    }

    /// <summary>
    /// 发生给不会改变地址的Actor
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entityId"></param>
    /// <param name="message"></param>
    public static void SendToStaticActor(this MessageLocationSender self, long entityId, IMessage message)
    {
        self.SendToStaticActorInner(entityId, message).Coroutine();
    }

    private static async ETTask SendToStaticActorInner(this MessageLocationSender self, long entityId, IMessage message)
    {
        var root = self.RootScene();
        var locationCache = self.GetOrCreateLocationCache(entityId);
        long instanceId = locationCache.InstanceId;

        using (await self.RootCoroutineLockComponent().WaitAsync(ECoroutineLockType.MessageLocationSender, entityId))
        {
            if (instanceId != locationCache.InstanceId)
            {
                throw new RpcException(ErrorCode.ERR_ACTOR_LOCATION_SEND_TIMEOUT, $"invalid instanceId: {instanceId}");
            }

            if (locationCache.ActorAddress == default)
            {
                locationCache.ActorAddress = await root.GetComponent<ActorNetworkLocationProxyComponent>().Get(self.GetLocationType(), entityId);
                
                if (instanceId != locationCache.InstanceId)
                {
                    throw new RpcException(ErrorCode.ERR_ACTOR_LOCATION_SEND_TIMEOUT_2, $"invalid instanceId: {instanceId}");
                }
            }
        }
        
        locationCache.LastSendOrReceiveTime =  TimeInfo.Instance.ServerNow();
        root.GetComponent<MessageSender>().Send(locationCache.ActorAddress, message);
    }

    /// <summary>
    /// 发生给不会改变地址的Actor
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entityId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="RpcException"></exception>
    public static async ETTask<IResponse> CallToStaticActorAsync(
        this MessageLocationSender self,
        long entityId,
        IRequest request)
    {
        var root = self.RootScene();
        var locationCache = self.GetOrCreateLocationCache(entityId);
        long instanceId = locationCache.InstanceId;

        using (await self.RootCoroutineLockComponent().WaitAsync(ECoroutineLockType.MessageLocationSender, entityId))
        {
            if (instanceId != locationCache.InstanceId)
            {
                throw new RpcException(ErrorCode.ERR_ACTOR_LOCATION_SEND_TIMEOUT, $"invalid instanceId: {instanceId}");
            }

            if (locationCache.ActorAddress == default)
            {
                locationCache.ActorAddress = await root.GetComponent<ActorNetworkLocationProxyComponent>().Get(self.GetLocationType(), entityId);
                
                if (instanceId != locationCache.InstanceId)
                {
                    throw new RpcException(ErrorCode.ERR_ACTOR_LOCATION_SEND_TIMEOUT_2, $"invalid instanceId: {instanceId}");
                }
            }
        }
        
        locationCache.LastSendOrReceiveTime =  TimeInfo.Instance.ServerNow();
        return await root.GetComponent<MessageSender>().Call(locationCache.ActorAddress, request);
    }

    /// <summary>
    /// 发送给可能改变地址的Actor，发送前会获取地址，发送失败会重试20次，每次间隔0.5s，重试期间如果InstanceId改变了说明Actor已经被删除了，直接放弃发送
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entityId"></param>
    /// <param name="message"></param>
    public static void Send(this MessageLocationSender self, long entityId, ILocationMessage message)
    {
        self.CallAsync(entityId, message).Coroutine();
    }
    

    /// <summary>
    /// 发送给可能改变地址的Actor，发送前会获取地址，发送失败会重试20次，每次间隔0.5s，重试期间如果InstanceId改变了说明Actor已经被删除了，直接放弃发送
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entityId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="RpcException"></exception>
    /// <exception cref="Exception"></exception>
    public static async ETTask<IResponse> CallAsync(
        this MessageLocationSender self,
        long entityId,
        ILocationRequest request)
    {
        var requestType = request.GetType();
        var locationCache = self.GetOrCreateLocationCache(entityId);
        long instanceId = locationCache.InstanceId;

        LogMsg.Instance.DebugInnerMsg(self, request, "Before MessageLocationSender");
        using (await self.RootCoroutineLockComponent().WaitAsync(ECoroutineLockType.MessageLocationSender, entityId))
        {
            LogMsg.Instance.DebugInnerMsg(self, request, "After MessageLocationSender");
            
            if (instanceId != locationCache.InstanceId)
            {
                throw new RpcException(ErrorCode.ERR_ACTOR_LOCATION_SEND_TIMEOUT, $"invalid instanceId: {instanceId}");
            }

            try
            {
                return await self.CallInnerAsync(locationCache, request);
            }
            catch (RpcException)
            {
                self.RemoveChild(entityId);
                throw;
            }
            catch (Exception e)
            {
                self.RemoveChild(entityId);
                throw new Exception($"{requestType.FullName}", e);
            }
        }
        
    }

    public static async ETTask<IResponse> CallInnerAsync(this MessageLocationSender self, ActorLocationCache locationCache, IRequest request)
    {
        var root = self.RootScene();
        var instanceId = locationCache.InstanceId;
        var requestType = request.GetType();
        locationCache.LastSendOrReceiveTime = TimeInfo.Instance.ServerNow();

        int retryTimes = 0;
        while (true)
        {
            if (locationCache.ActorAddress == default)
            {
                locationCache.ActorAddress = await root.GetComponent<ActorNetworkLocationProxyComponent>().Get(self.GetLocationType(), locationCache.Id);
                
                if (instanceId != locationCache.InstanceId)
                {
                    throw new RpcException(ErrorCode.ERR_ACTOR_LOCATION_SEND_TIMEOUT_3, $"invalid instanceId: {instanceId}");
                }
            }

            if (locationCache.ActorAddress == default)
            {
                var errorResponse = (IResponse)ObjectPool.Instance.Fetch(MessageOpcodeManager.Instance.GetResponseTypeBy(requestType));
                errorResponse.Error = ErrorCode.ERR_ACTOR_NOT_FOUND;
                return errorResponse;
            }

            var response = await root.GetComponent<MessageSender>().Call(locationCache.ActorAddress, request, needException: false);

            if (response.Error == ErrorCode.ERR_ACTOR_NOT_FOUND)
            {
                // 如果是ERR_ACTOR_NOT_FOUND，说明地址不对了，可能是Actor迁移了，这时候重试获取地址并发送，重试20次还不行就放弃
                retryTimes++;

                if (retryTimes > 20)
                {
                    Log.Debug($"actor send message fail, actorid: {locationCache.Id} {requestType.FullName}");
                            
                    // 这里删除actor，后面等待发送的消息会判断InstanceId，InstanceId不一致返回ERR_NotFoundActor
                    self.RemoveChild(locationCache.Id);
                    return response;
                }
                
                // 等待0.5s再发送
                await root.GetComponent<TimerComponent>().WaitDurationAsync(500);
                if (locationCache.InstanceId != instanceId)
                {
                    throw new RpcException(ErrorCode.ERR_ACTOR_LOCATION_SEND_TIMEOUT_4, $"{requestType.FullName}");
                }

                locationCache.ActorAddress = default;
                
                continue;
            }
            if (response.Error == ErrorCode.ERR_TASK_TIMEOUT)
            {
                throw new RpcException(response.Error, $"{requestType.FullName}");
            }

            
            if (ErrorCode.IsRpcNeedThrowException(response.Error))
            {
                throw new RpcException(response.Error, $"Message: {response.Message} Request: {requestType.FullName}");
            }
            
            return response;
        }
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetLocationType(this MessageLocationSender self)
    {
        return (int)self.Id;
    }
}

[Invoke(TimerInvokeType.MessageLocationSenderCheck)]
public class MessageLocationSenderSystems_CheckTimer : ATimer<MessageLocationSender>
{
    protected override void Run(MessageLocationSender self)
    {
        self.Check();
    }
}

public static class MessageLocationSenderManagerSystems
{
    public static MessageLocationSender Get(this MessageLocationSenderManager self, int locationType)
    {
        var sender = self.GetChild<MessageLocationSender>(locationType);
        if (sender == null)
        {
            sender = self.AddChildWithId<MessageLocationSender>(locationType);
        }
        return sender;
    }
}

[EntitySystemOf(typeof(ActorLocationCache))]
public static partial class ActorLocationCacheSystems
{
    private static void Awake(this ActorLocationCache self)
    {
        self.LastSendOrReceiveTime = TimeInfo.Instance.ServerNow();
        self.ActorAddress = default;
    }
    private static void Destroy(this ActorLocationCache self)
    {
        self.LastSendOrReceiveTime = 0;
        self.ActorAddress = default;
    }
}