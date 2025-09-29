namespace Bliss.Timer
{
    internal struct TimerAction
    {
        public ETimerType TimerType;
        
        /// <summary>
        /// 开始时间 MS
        /// </summary>
        public long StartTimeMS;
        
        /// <summary>
        /// 持续时间 MS
        /// </summary>
        public long DurationMS;
        
        /// <summary>
        /// 计时器处理器类型标识
        /// </summary>
        public int TimerHandlerIdentifier;
        
        
        public object UserData;
        
        
        public TimerAction(ETimerType timerType, long startTimeMS, long durationMS, int timerHandlerIdentifier, object userData)
        {
            TimerType = timerType;
            StartTimeMS = startTimeMS;
            DurationMS = durationMS;
            TimerHandlerIdentifier = timerHandlerIdentifier;
            UserData = userData;
        }
    }
}