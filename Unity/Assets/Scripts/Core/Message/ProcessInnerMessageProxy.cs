using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// 相同进程内的消息代理，主要用于Actor之间的通信
    /// 不要直接使用这个类，应该通过<see cref="MessageSender"/> or <see cref="MessageLocationSender"/>来发送消息
    /// </summary>
    public class ProcessInnerMessageProxy : Entity, IAwake, IDestroy, IUpdate
    {
        public List<ProcessMessageInfo> Messages = new();
        public const long TimeOutMS =  40 * 1000;
        public int RpcIdGenerator;
        public Dictionary<int, ProcessInnerPrcInfo> RpcInfos = new Dictionary<int, ProcessInnerPrcInfo>();
    }

    public struct ProcessInnerPrcInfo
    {
        public ActorAddress ActorAddress { get; }
        public Type RequestType => RpcInfo.RequestType;
        public bool NeedException { get; }
        public RpcInfo RpcInfo { get; }

        public ProcessInnerPrcInfo(ActorAddress actorAddress, Type requestType, bool needException)
        {
            ActorAddress = actorAddress;
            NeedException = needException;

            RpcInfo = new RpcInfo(requestType);
        }
    }
}