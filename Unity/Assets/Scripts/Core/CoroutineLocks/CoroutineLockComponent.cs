using System;
using System.Collections.Generic;
using Bliss.Core;
using Bliss.CoroutineLocks;
using Codice.Client.Commands.WkTree;
using ET;

namespace Bliss
{
    [EntitySystemOf(typeof(CoroutineLockComponent))]
    public static partial class CoroutineLockComponentSystems
    {
        public static void Awake(this CoroutineLockComponent self)
        {

        }
        
        public static void Update(this CoroutineLockComponent self)
        {
            // 循环过程中会有对象继续加入队列
            while (self.NextFrameRun.Count > 0)
            {
                (int coroutineLockType, long coroutineLockTypeIdentifier, int lockDepth) = self.NextFrameRun.Dequeue();
                self.Notify(coroutineLockType, coroutineLockTypeIdentifier, lockDepth);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="coroutineLockType"></param>
        /// <param name="coroutineLockIdentifier"></param>
        /// <param name="timeoutMS">超时时间 MS</param>
        /// <returns></returns>
        public static async ETTask<CoroutineLock> WaitAsync(this CoroutineLockComponent self,
            int coroutineLockType, int coroutineLockIdentifier,
            int timeoutMS = 60000)
        {
            CoroutineLockQueueBucket coroutineLockQueueBucket = self.GetChild<CoroutineLockQueueBucket>(coroutineLockType) ?? self.AddChildWithId<CoroutineLockQueueBucket>(coroutineLockType);
            return await coroutineLockQueueBucket.WaitAsync(coroutineLockIdentifier, timeoutMS);
        }
        
        private static void Notify(this CoroutineLockComponent self, int coroutineLockType, long coroutineLockTypeIdentifier, int lockDepth)
        {
            CoroutineLockQueueBucket coroutineLockQueueType = self.GetChild<CoroutineLockQueueBucket>(coroutineLockType);
            if (coroutineLockQueueType == null)
            {
                return;
            }
            coroutineLockQueueType.Notify(coroutineLockTypeIdentifier, lockDepth);
        }
        
        internal static void RunNextCoroutine(this CoroutineLockComponent self, int coroutineLockType, long coroutineLockIdentifier, int lockDepth)
        {
            // 一个协程队列一帧处理超过100个,说明比较多了,打个warning,检查一下是否够正常
            if (lockDepth == 100)
            {
                Log.Warning($"too much coroutine level: {coroutineLockType} {coroutineLockIdentifier}");
            }

            self.NextFrameRun.Enqueue((coroutineLockType, coroutineLockIdentifier, lockDepth));
        }
    }

    public class CoroutineLockComponent : Entity, IAwake, IUpdate
    {
        /// <summary>
        /// coroutineLockType, coroutineLockIdentifier, lockDepth
        /// </summary>
        public readonly Queue<(int, long, int)> NextFrameRun = new();
    }
}