using Bliss.Core.EventSystem;

namespace Bliss;

/// <summary>
/// 发送给GateSession的消息直接转发给客户端
/// <seealso cref="MapMessageHelper.SendToClient"/>
/// </summary>
[Invoke((int)EMailBoxType.GateSession)]
public class MailboxMessageHandler_GateSession : AInvokeHandler<MailBoxInvoker>
{
    public override void Handle(MailBoxInvoker args)
    {
       if(args.MailBoxComponent.Parent is PlayerSessionComponent playerSessionComponent)
       {
           playerSessionComponent.Session?.Send(args.MessageObject);
       }
    }
}