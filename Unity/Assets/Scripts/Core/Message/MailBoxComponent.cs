using System;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Pool;
using ET;

namespace Bliss
{
    /// <summary>
    /// <seealso cref="NetworkEntityManagerComponent"/>
    /// </summary>
    public class MailBoxComponent : Entity, IAwake<EMailBoxType>, IDestroy
    {
        public long ParentInstanceId;
        public EMailBoxType MailBoxType;
    }

    public enum EMailBoxType
    {
        OrderedMessage = 1,
        UnOrderedMessage,
        GateSession,
    }
    
    public struct MailBoxInvoker
    {
        public RootAddress FromAddress;
        public MessageObject MessageObject;
        public MailBoxComponent MailBoxComponent;
    }

    [EntitySystemOf(typeof(MailBoxComponent))]
    public static partial class MailBoxComponentSystems
    {
        private static void Awake(this MailBoxComponent self, EMailBoxType param1)
        {
            self.ParentInstanceId = self.Parent.InstanceId;
            self.MailBoxType = param1;
            self.RootScene().GetComponent<NetworkEntityManagerComponent>()?.AddEntityMailBox(self.ParentInstanceId, self);
        }
        private static void Destroy(this MailBoxComponent self)
        {
            self.RootScene()?.GetComponent<NetworkEntityManagerComponent>()?.RemoveEntityMailBox(self.ParentInstanceId);
        }

        /// <summary>
        /// 根据mailboxType进行分发处理
        /// </summary>
        /// <param name="self"></param>
        /// <param name="fromAddress">发信地址</param>
        /// <param name="message"></param>
        public static void Delivery(this MailBoxComponent self, RootAddress fromAddress, MessageObject message)
        {
            EventSystem.Instance.Invoke((int)self.MailBoxType, new MailBoxInvoker()
            {
                FromAddress = fromAddress,
                MessageObject = message,
                MailBoxComponent = self
            });
            
            // if (self.MailBoxType == EMailBoxType.OrderedMessage)
            // {
            //     self.HandleOrderedMessage(fromAddress, message).Coroutine();
            // }
            // else if(self.MailBoxType == EMailBoxType.UnOrderedMessage)
            // {
            //     self.HandleUnOrderedMessage(fromAddress, message).Coroutine();
            // }
        }

        // public static async ETTask HandleOrderedMessage(this MailBoxComponent self, RootAddress fromAddress, MessageObject message)
        // {
        //     long instanceId = self.InstanceId;
        //     using (await self.RootCoroutineLockComponent().WaitAsync((int)ECoroutineLockType.Mailbox, self.ParentInstanceId))
        //     {
        //         if (instanceId != self.InstanceId)
        //         {
        //             return;
        //         }
        //         await MessageDispatcher.Instance.HandleMessageAsync(self.Parent, fromAddress, message);
        //     }
        // }
        //
        // public static async ETTask HandleUnOrderedMessage(this MailBoxComponent self, RootAddress fromAddress,MessageObject message)
        // {
        //     await MessageDispatcher.Instance.HandleMessageAsync(self.Parent, fromAddress, message);
        // }
    }

    [Invoke((int)EMailBoxType.UnOrderedMessage)]
    public class MailboxMessageHandler_UnOrderedMessage : AInvokeHandler<MailBoxInvoker>
    {
        public override void Handle(MailBoxInvoker args)
        {
            HandleUnOrderedMessage(args.MailBoxComponent, args.FromAddress, args.MessageObject).Coroutine();
        }
        
        private async ETTask HandleUnOrderedMessage(MailBoxComponent self, RootAddress fromAddress,MessageObject message)
        {
            await MessageDispatcher.Instance.HandleMessageAsync(self.Parent, fromAddress, message);
        }
    }
    
    [Invoke((int)EMailBoxType.OrderedMessage)]
    public class MailboxMessageHandler_OrderedMessage : AInvokeHandler<MailBoxInvoker>
    {
        public override void Handle(MailBoxInvoker args)
        {
            HandleOrderedMessage(args.MailBoxComponent, args.FromAddress, args.MessageObject).Coroutine();
        }
        
        private async ETTask HandleOrderedMessage(MailBoxComponent self, RootAddress fromAddress,MessageObject message)
        {
            long instanceId = self.InstanceId;
            using (await self.RootCoroutineLockComponent().WaitAsync((int)ECoroutineLockType.Mailbox, self.ParentInstanceId))
            {
                if (instanceId != self.InstanceId)
                {
                    if(message is IRequest request)
                    {
                        IResponse response = MessageOpcodeManager.Instance.CreateResponseBy(request.GetType(), request.RpcId, ErrorCode.ERR_ACTOR_NOT_FOUND);
                        self.RootScene().GetComponent<ProcessInnerMessageProxy>().Reply(fromAddress, response);
                    }
                    return;
                }
                await MessageDispatcher.Instance.HandleMessageAsync(self.Parent, fromAddress, message);
            }
        }
    }
}