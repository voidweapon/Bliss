using System.Collections.Generic;

namespace Bliss;

/// <summary>
/// 知道ActorAddress时用这个发生消息
/// </summary>
public class MessageSender : Entity
{
    public const long TimeOutMS = 40 * 1000;
    public Dictionary<int, EntityRef<Session>> DicProcessId2SessionRef = new Dictionary<int, EntityRef<Session>>();
}