using System;

namespace Bliss
{
    public class TimeInfo
    {
        
        private static TimeInfo instance;
        public static TimeInfo Instance
        {
            get
            {
                if (instance == null) 
                    instance = new TimeInfo(); 
                return instance;
            }
        }
        
        private DateTime dt1970;
        private DateTime dt;
        
        
        private int timeZone;
        public int TimeZone
        {
            get
            {
                return this.timeZone;
            }
            set
            {
                this.timeZone = value;
                dt = dt1970.AddHours(TimeZone);
            }
        }

        
        // ping消息会设置该值，原子操作
        public long ServerMinusClientTime { private get; set; }

        public long FrameTime { get; private set; }

        private long StartTicks;
        public float RealTimeSecondSinceStartup  { get; private set; }
        

        public void Init()
        {
            StartTicks = DateTime.UtcNow.Ticks;
        }
        
        
        public void Update()
        {
            // 赋值long型是原子操作，线程安全
            this.FrameTime = this.ClientNow();
            RealTimeSecondSinceStartup = (DateTime.UtcNow.Ticks - StartTicks) / (float)TimeSpan.TicksPerSecond;
        }
        
        /// <summary> 
        /// 根据时间戳获取时间 
        /// </summary>  
        public DateTime ToDateTime(long timeStamp)
        {
            return dt.AddTicks(timeStamp * 10000);
        }
        
        // 线程安全
        public long ClientNow()
        {
            return (DateTime.UtcNow.Ticks - this.dt1970.Ticks) / 10000;
        }
        
        public long ServerNow()
        {
            return ClientNow() + this.ServerMinusClientTime;
        }
        
        public long ClientFrameTime()
        {
            return this.FrameTime;
        }
        
        public long ServerFrameTime()
        {
            return this.FrameTime + this.ServerMinusClientTime;
        }
        
        public long Transition(DateTime d)
        {
            return (d.Ticks - dt.Ticks) / 10000;
        }
    }
}