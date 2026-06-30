namespace Bliss;


public class MessageLocationSenderManager : Entity
{
    
}

/// <summary>
/// 只知道Entity的Id时用这个发送消息
/// <seealso cref="MessageSender"/>
/// </summary>
public class MessageLocationSender : Entity, IAwake, IDestroy
{
    public long CheckTimerId;
}

/// <summary>
/// Actor所在的地址缓存
/// </summary>
public class ActorLocationCache : Entity, IAwake, IDestroy
{
    /// <summary>
    /// Actor所在的地址
    /// </summary>
    public ActorAddress ActorAddress;
    
    /// <summary>
    /// 最后一次发送或接收消息的时间戳，单位毫秒
    /// </summary>
    public long LastSendOrReceiveTime;
}