using Bliss.Core;
using ET;

namespace Bliss
{
    public class MailBoxComponent : Entity, IAwake<EMailBoxType>, IDestroy
    {
        public long ParentInstanceId;
        public EMailBoxType MailBoxType;
    }

    public enum EMailBoxType
    {
        OrderedMessage = 1,
        UnOrderedMessage,
    }

    [EntitySystemOf(typeof(MailBoxComponent))]
    public static partial class MailBoxComponentSystems
    {
        public static void Awake(this MailBoxComponent self, EMailBoxType param1)
        {
            self.ParentInstanceId = self.Parent.InstanceId;
            self.MailBoxType = param1;
        }
        public static void Destroy(this MailBoxComponent self)
        {

        }

        public static void Delivery(this MailBoxComponent self, MessageObject message)
        {
            if (self.MailBoxType == EMailBoxType.OrderedMessage)
            {
                self.HandleOrderedMessage(message).Coroutine();
            }
            else if(self.MailBoxType == EMailBoxType.UnOrderedMessage)
            {
                self.HandleUnOrderedMessage(message).Coroutine();
            }
        }

        public static async ETTask HandleOrderedMessage(this MailBoxComponent self, MessageObject message)
        {
            long instanceId = self.InstanceId;
            using (await self.RootCoroutineLockComponent().WaitAsync((int)ECoroutineLockType.Mailbox, self.ParentInstanceId))
            {
                if (instanceId != self.InstanceId)
                {
                    return;
                }
                await MessageDispatcher.Instance.HandleMessageAsync(self.Parent, message);
            }
        }

        public static async ETTask HandleUnOrderedMessage(this MailBoxComponent self, MessageObject message)
        {
            await MessageDispatcher.Instance.HandleMessageAsync(self.Parent, message);
        }
    }
}