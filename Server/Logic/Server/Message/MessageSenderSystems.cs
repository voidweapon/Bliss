using Bliss.Core;
using ET;

namespace Bliss;

public static class MessageSenderSystems
{
    public static void Send(this MessageSender self, ActorAddress actorAddress, IMessage message)
    {
        var msgObj = message as MessageObject;
        if (msgObj == null)
        {
            throw new System.ArgumentNullException(nameof(message));
        }
        
        //同进程消息
        if(actorAddress.Process == self.Root().Process)
        {
            self.RootScene().GetComponent<ProcessInnerMessageProxy>().Send(actorAddress, msgObj);
            return;
        }
        
        //不同进程消息
        
        //获取目标进程的Session
        var session = self.GetSession(actorAddress.Process);
        // 回包时需要知道发向哪个进程
        actorAddress.Process = self.Root().Process; 
        session.Send(msgObj);
    }
    
    
    public static async ETTask<IResponse> Call(
        this MessageSender self,
        ActorAddress actorAddress,
        IRequest request,
        long timeOutMs = MessageSender.TimeOutMS,
        bool needException = true
    )
    {
        
        IResponse response = null;
        if(actorAddress.Process == self.Root().Process)
        {
            //同进程消息
            response =  await self.RootScene().GetComponent<ProcessInnerMessageProxy>().Call(actorAddress, request, needException);
        }
        else
        {
            //不同进程消息
            //获取目标进程的Session
            var session = self.GetSession(actorAddress.Process);
            // 回包时需要知道发向哪个进程
            actorAddress.Process = self.Root().Process; 
            response = await session.Call(actorAddress, request, timeOutMs, null, needException);
        }

        if (response.Error == ErrorCode.ERR_TASK_TIMEOUT)
        {
            throw new RpcException(response.Error, $"Rpc error: request, 注意Actor消息超时，请注意查看是否死锁或者没有reply: actorId: {actorAddress} {request}, response: {response}");
        }
        if (needException && ErrorCode.IsRpcNeedThrowException(response.Error))
        {
            throw new RpcException(response.Error, $"Rpc error: actorId: {actorAddress} {request}, response: {response}");
        }
        
        return response;
    }

    public static Session GetSession(this MessageSender self, int process)
    {
        if (!self.DicProcessId2SessionRef.TryGetValue(process, out var sessionRef))
        {
            var networkComponent = self.RootScene().GetComponent<NetworkComponent>();
            var processCfg = ConfigManager.Instance.Tables.TbServerProcess.Get(process);

            var session = networkComponent.CreateConnection(processCfg.InnerIpInnerPort);
            self.DicProcessId2SessionRef.Add(process, session);
            sessionRef = session;
        }
        return (Session)sessionRef;
    }
}