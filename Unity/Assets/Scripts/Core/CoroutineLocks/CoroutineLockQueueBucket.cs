using Bliss.Core;
using ET;

namespace Bliss.CoroutineLocks
{
    public class CoroutineLockQueueBucket : Entity, IAwake
    {

    }


    [EntitySystemOf(typeof(CoroutineLockQueueBucket))]
    public static partial class CoroutineLockQueueBucketSystem
    {
        public static void Awake(this CoroutineLockQueueBucket self)
        {

        }

        private static CoroutineLockQueue CreateQueue(this CoroutineLockQueueBucket self, long key)
        {
            CoroutineLockQueue newQueue = self.AddChildWithId<CoroutineLockQueue, int>(key, (int)self.Id, true);
            return newQueue;
        }
        
        private static CoroutineLockQueue GetQueue(this CoroutineLockQueueBucket self, long key)
        {
            return self.GetChild<CoroutineLockQueue>(key);
        }
        
        private static void RemoveQueue(this CoroutineLockQueueBucket self, long key)
        {
            self.RemoveChild(key);
        }

        internal static async ETTask<CoroutineLock> WaitAsync(this CoroutineLockQueueBucket self, long coroutineLockIdentifier,
            long timeoutMS = 60000)
        {
            CoroutineLockQueue coroutineLockQueue = self.GetQueue(coroutineLockIdentifier) ?? self.CreateQueue(coroutineLockIdentifier);
            return await coroutineLockQueue.WaitAsync(timeoutMS);
        }

        internal static void Notify(this CoroutineLockQueueBucket self, long coroutineLockTypeIdentifier, int lockDepth)
        {
            CoroutineLockQueue queue = self.GetQueue(coroutineLockTypeIdentifier);
            if (queue == null)
            {
                return;
            }

            if (!queue.Notify(lockDepth))
            {
                self.RemoveQueue(coroutineLockTypeIdentifier);
            }
        }
    }


}