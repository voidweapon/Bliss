using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;
using ET;

namespace Bliss.Timer
{
    public class TimerComponent : Entity, IAwake, IUpdate
    {
        /// <summary>
        /// 最小的过期时间
        /// </summary>
        internal long MinTimeout = long.MaxValue;

        /// <summary>
        /// 用于Timer Identifier 生成
        /// </summary>
        internal long IdGenerator = 0;
        
        /// <summary>
        /// TimeStamp -> Timer Id List
        /// </summary>
        internal MultiMapList<long, long> TimeBucket =  new MultiMapList<long, long>(1000);

        /// <summary>
        /// Timer Id -> Timer 
        /// </summary>
        internal Dictionary<long, TimerAction> TimerActionMap = new Dictionary<long, TimerAction>();
        
        /// <summary>
        /// 短时计时器
        /// 过期时间为一帧的计时器
        /// </summary>
        internal Dictionary<long, TimerAction> LateTimerActionMap = new Dictionary<long, TimerAction>();
        internal Queue<long> LateTimerIds = new Queue<long>();
        
        /// <summary>
        /// Temp collections for record timeout time
        /// </summary>
        internal Queue<long> TimeoutTimes = new Queue<long>();
        
        /// <summary>
        /// Temp collections for record timeout Timer's Identifier
        /// </summary>
        internal Queue<long> TimeoutTimerIds = new Queue<long>();
        
    }

    [EntitySystemOf(typeof(TimerComponent))]
    public static partial class TimerComponentSystems
    {
        public static void Awake(this TimerComponent self)
        {

        }
        
        public static void Update(this TimerComponent self)
        {
            if (self.TimeBucket.Count == 0)
            {
                return;
            }
            
            long timeNow = self.GetNow();
            if (timeNow < self.MinTimeout)
            {
                return;
            }

            //寻找过期的时间点
            foreach (var kv in self.TimeBucket)
            {
                long time = kv.Key;
                if (time > timeNow)
                {
                    self.MinTimeout =  time;
                    break;
                }
                
                self.TimeoutTimes.Enqueue(time);
            }

            //寻找过期的Timer
            while (self.TimeoutTimes.Count > 0)
            {
                var time = self.TimeoutTimes.Dequeue();
                var timerIds = self.TimeBucket[time];
                foreach (var timerId in timerIds)
                {
                    self.TimeoutTimerIds.Enqueue(timerId);
                }

                self.TimeBucket.Remove(time);
            }

            if (self.TimeBucket.Count == 0)
            {
                self.MinTimeout = long.MaxValue;
            }

            //通知Timer过期了
            while (self.TimeoutTimerIds.Count > 0)
            {
                var timerId = self.TimeoutTimerIds.Dequeue();

                if (!self.TimerActionMap.Remove(timerId, out var timerAction))
                {
                    continue;
                }
                
                self.Run(timerId, ref timerAction);
            }
        }

        private static void LateUpdateTImer(this TimerComponent self)
        {
            int thisTickCount = self.LateTimerIds.Count;
            for (int i = 0; i < thisTickCount; i++)
            {
                var timerId = self.LateTimerIds.Dequeue();
                if (self.LateTimerActionMap.TryGetValue(timerId, out var timerAction))
                {
                    self.Run(timerId, ref timerAction);
                }
            }
        }

        /// <summary>
        /// 开启无限计时器， 执行间隔<see cref="intervalMS"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="intervalMS"></param>
        /// <param name="timerHandleType"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public static long NewRepeatedTimer(this TimerComponent self, long intervalMS, int timerHandleType, object userData)
        {
            self.EnsureInterval(intervalMS);
            
            long timeNow = self.GetNow();
            long timerId = self.GetId();
            TimerAction timer = new TimerAction(ETimerType.RepeatedTimer, timeNow, intervalMS, timerHandleType, userData);
            
            self.AddTimer(timerId, ref timer);
            return timerId;
        }

        /// <summary>
        /// 开启一次性计时器直到到达过过期时间(<see cref="expireAtMS"/>)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="expireAtMS">过期时间戳</param>
        /// <param name="timerHandleType"></param>
        /// <param name="userData"></param>
        /// <returns>Timer Id</returns>
        public static long NewOnceTimerUntil(this TimerComponent self, long expireAtMS, int timerHandleType, object userData)
        {
            long timeNow = self.GetNow();

            long intervalMS = expireAtMS - timeNow;
            self.EnsureInterval(intervalMS);
            
            long timerId = self.GetId();
            TimerAction timer = new TimerAction(ETimerType.OnceTimer, timeNow, intervalMS, timerHandleType, userData);
            
            self.AddTimer(timerId, ref timer);
            return timerId;
        }
        
        /// <summary>
        /// 开启一次性计时器持续<see cref="durationMS"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="durationMS">持续时间</param>
        /// <param name="timerHandleType"></param>
        /// <param name="userData"></param>
        /// <returns>Timer Id</returns>
        public static long NewOnceTimer(this TimerComponent self, long durationMS, int timerHandleType, object userData)
        {
            self.EnsureInterval(durationMS);
            long timeNow = self.GetNow();
            long timerId = self.GetId();
            TimerAction timer = new TimerAction(ETimerType.OnceTimer, timeNow, durationMS, timerHandleType, userData);
            
            self.AddTimer(timerId, ref timer);
            return timerId;
        }
        
        /// <summary>
        /// 等待直到到达过期时间
        /// </summary>
        /// <param name="self"></param>
        /// <param name="expireAtMS">过期时间戳</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True: Timer Execute;  False: Timer Canceled</returns>
        public static async ETTask<bool> WaitUntilAsync(this TimerComponent self, long expireAtMS,
            ETCancellationToken cancellationToken = null)
        {
            long timeNow = self.GetNow();
            if (timeNow >= expireAtMS)
            {
                return true;
            }
            
            long intervalMS = expireAtMS - timeNow;
            self.EnsureInterval(intervalMS);

            var tcs = ETTask<bool>.Create(true);
            long timerId = self.GetId();
            TimerAction timer = new(ETimerType.OnceWaitTimer, timeNow, intervalMS, 0, tcs);
            self.AddTimer(timerId, ref timer);

            void CancelAction()
            {
                if (self.Remove(timerId))
                {
                    tcs.SetResult(false);
                }
            }

            try
            {
                cancellationToken?.Add(CancelAction);
                return await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }
        }
        
        /// <summary>
        /// 等待<see cref="durationMS"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="durationMS">持续时间</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True: Timer Execute;  False: Timer Canceled</returns>
        public static async ETTask<bool> WaitDurationAsync(this TimerComponent self, long durationMS,
            ETCancellationToken cancellationToken = null)
        {
            long timeNow = self.GetNow();
            if (durationMS <= 0)
            {
                return true;
            }
            
            self.EnsureInterval(durationMS);

            var tcs = ETTask<bool>.Create(true);
            long timerId = self.GetId();
            TimerAction timer = new TimerAction(ETimerType.OnceWaitTimer, timeNow, durationMS, 0, tcs);
            self.AddTimer(timerId, ref timer);

            void CancelAction()
            {
                if (self.Remove(timerId))
                {
                    tcs.SetResult(false);
                }
            }

            try
            {
                cancellationToken?.Add(CancelAction);
                return await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }
        }
        
        public static async ETTask<bool> WaitNextFrameAsync(this TimerComponent self,
            ETCancellationToken cancellationToken = null)
        {
            long timeNow = self.GetNow();
            
            var tcs = ETTask<bool>.Create(true);
            long timerId = self.GetId();
            TimerAction timer = new TimerAction(ETimerType.OnceWaitTimer, timeNow, 0, 0, tcs);

            self.LateTimerActionMap.Add(timerId, timer);
            
            void CancelAction()
            {
                if (self.LateTimerActionMap.Remove(timerId))
                {
                    tcs.SetResult(false);
                }
            }

            try
            {
                cancellationToken?.Add(CancelAction);
                return await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }
        }

        public static bool CancelTimer(this TimerComponent self, ref long timerId)
        {
            long id = timerId;
            timerId = 0;

            return self.Remove(id);
        }
        
        private static void Run(this TimerComponent self, long timerId, ref TimerAction timerAction)
        {
            switch (timerAction.TimerType)
            {
                case ETimerType.OnceTimer:
                    EventSystem.Instance.Invoke(timerAction.TimerHandlerIdentifier, new TimerCallback() { Userdata = timerAction.UserData });
                    break;
                case ETimerType.OnceWaitTimer:
                    ETTask<bool> tcs = timerAction.UserData as ETTask<bool>;
                    tcs.SetResult(true);
                    break;
                case ETimerType.RepeatedTimer:
                    long timeNow = self.GetNow();
                    timerAction.StartTimeMS = timeNow;
                    self.AddTimer(timerId, ref timerAction);
                    EventSystem.Instance.Invoke(timerAction.TimerHandlerIdentifier, new TimerCallback() { Userdata = timerAction.UserData });
                    break;
            }
        }

        private static void AddTimer(this TimerComponent self, long timerId, ref TimerAction timer)
        {
            long expireTime = timer.StartTimeMS + timer.DurationMS;
            self.TimeBucket.Add(expireTime, timerId);
            self.TimerActionMap.Add(timerId, timer);
            
            if (expireTime < self.MinTimeout)
            {
                self.MinTimeout = expireTime;
            }
        }
        
        private static bool Remove(this TimerComponent self, long id)
        {
            if (id == 0)
            {
                return false;
            }
            
            return self.TimerActionMap.Remove(id);
        }

        private static long GetNow(this TimerComponent self)
        {
            return TimeInfo.Instance.ServerFrameTime();
        }
        
        private static long GetId(this TimerComponent self)
        {
            return ++self.IdGenerator;
        }

        private static void EnsureInterval(this TimerComponent self, long intervalMS)
        {
            
        }
    }
}