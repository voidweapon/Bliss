using System.Collections.Generic;
using Bliss.Core;
using Bliss.Timer;
using ET;

namespace Bliss.CoroutineLocks
{
    public class CoroutineLockQueue : Entity, IAwake<int>, IDestroy
    {
        public int CoroutineLockType;
        public bool IsStart;
        
        internal readonly Queue<WaitCoroutineLock> LockQueue = new();
    }

    [EntitySystemOf(typeof(CoroutineLockQueue))]
    public static partial class CoroutineLockQueueSystems
    {
        private static void Awake(this CoroutineLockQueue self, int coroutineLockType)
        {
            self.IsStart = false;
            self.CoroutineLockType = coroutineLockType;
        }

        private static void Destroy(this CoroutineLockQueue self)
        {
            self.LockQueue.Clear();
            self.IsStart = false;
            self.CoroutineLockType = 0;;
        }

        internal static async ETTask<CoroutineLock> WaitAsync(this CoroutineLockQueue self, long timeoutMS = 60000)
        {
            CoroutineLock coroutineLock = null;
            if (!self.IsStart)
            {
                self.IsStart = true;
                coroutineLock = self.AddChild<CoroutineLock, CoroutineLockInitParam>(new CoroutineLockInitParam()
                {
                    LockType = self.CoroutineLockType,
                    Identifier = self.Id,
                    LockDepth = 1,
                }, true);
                return coroutineLock;
            }

            WaitCoroutineLock waitCoroutineLock = WaitCoroutineLock.Create();
            self.LockQueue.Enqueue(waitCoroutineLock);
            if (timeoutMS > 0)
            {
                long expireMS = TimeInfo.Instance.ClientFrameTime() + timeoutMS;
                self.RootTimerComponent().NewOnceTimer(expireMS, TimerCoreInvokeType.CoroutineTimeout, waitCoroutineLock);
            }
            coroutineLock = await waitCoroutineLock.Wait();
            return coroutineLock;
        }

        // 返回值，是否找到了一个有效的协程锁
        internal static bool Notify(this CoroutineLockQueue self, int lockDepth)
        {
            // 有可能WaitCoroutineLock已经超时抛出异常，所以要找到一个未处理的WaitCoroutineLock
            while (self.LockQueue.Count > 0)
            {
                WaitCoroutineLock waitCoroutineLock = self.LockQueue.Dequeue();

                if (waitCoroutineLock.IsDisposed())
                {
                    continue;
                }

                CoroutineLock coroutineLock = self.AddChild<CoroutineLock, CoroutineLockInitParam>(new CoroutineLockInitParam()
                {
                    LockType = self.CoroutineLockType,
                    Identifier = self.Id,
                    LockDepth = lockDepth,
                }, true);

                waitCoroutineLock.SetResult(coroutineLock);
                return true;
            }
            return false;
        }
    }
}