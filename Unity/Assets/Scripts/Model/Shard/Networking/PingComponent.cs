namespace Bliss
{
    public class PingComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public bool IsUpdateServerTime = false;
        
        public const int OutLagPacketCountLimit = 265;
        public int[] OutLagPacketId;
        public long[] OutLagPacketSendTime;
        public int OutPacketId;
        
        public long PingIntervalMS = 100;
        public long LastPingTimeMS;

        public const int RTT_MA_Window = 50;
        public ExponentialMovingAverage RTT_EMA;
        public ExponentialMovingAverage Jitter_EMA;
    }
}