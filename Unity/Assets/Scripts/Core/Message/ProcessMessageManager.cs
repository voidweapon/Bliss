using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Bliss
{
    public class ProcessMessageManager : Singleton<ProcessMessageManager>
    {
        private readonly ConcurrentDictionary<int, ConcurrentQueue<ProcessMessageInfo>> _messageQueues = new();

        public bool Enqueue(ActorAddress targetAddress, MessageObject message)
        {
            return Enqueue(targetAddress.RootAddress, targetAddress, message);
        }
    
        public bool Enqueue(RootAddress fromAddress, ActorAddress targetAddress, MessageObject message)
        {
            if(!_messageQueues.TryGetValue(targetAddress.Root, out var queue))
            {
                return false;
            }
        
            queue.Enqueue(new ProcessMessageInfo() { TargetAddress = new ActorAddress(fromAddress, targetAddress.InstanceId),  MessageObject = message });
            return true;
        }

        public void Fetch(int processId, List<ProcessMessageInfo> list, int maxCount)
        {
            if(!_messageQueues.TryGetValue(processId, out var queue))
            {
                return;
            }

            for (int i = 0; i < maxCount; i++)
            {
                if (!queue.TryDequeue(out var info))
                {
                    break;
                }
            
                list.Add(info);
            }
        }

        public void Register(int processId)
        {
            _messageQueues.TryAdd(processId, new ConcurrentQueue<ProcessMessageInfo>());
        }

        public void Unregister(int processId)
        {
            _messageQueues.TryRemove(processId, out _);
        }
    }

    public struct ProcessMessageInfo
    {
        public ActorAddress TargetAddress;
        public MessageObject MessageObject;
    }
}