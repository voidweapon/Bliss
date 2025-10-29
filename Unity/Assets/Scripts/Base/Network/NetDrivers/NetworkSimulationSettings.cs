using Unity.Mathematics;

namespace Bliss.Core.Network
{
    public struct NetworkSimulationSettings
    {
        /// <summary>
        /// 丢包率
        /// [0,100]
        /// </summary>
        public int Loss;
        /// <summary>
        /// 小于这个大小的包会进入丢包策略
        /// </summary>
        public int LossMinPktSize;
        /// <summary>
        /// 大于这个大小的包会进入丢包策略
        /// </summary>
        public int LossMaxPktSize;

        /// <summary>
        /// 启用包乱序
        /// </summary>
        public bool Disorder;

        /// <summary>
        /// 包重复概率
        /// </summary>
        public int Duplicate;
        
        /// <summary>
        /// Ping值 毫秒
        /// </summary>
        public int Latency;
        public int LatencyVariance;
        public int LatencyMin;
        public int LatencyMax;

        public int Jitter;

        public bool ShouldDropPacketOfSize(int numBytes)
        {
            return numBytes < LossMinPktSize || numBytes > LossMaxPktSize;
        }
        
        public bool ParseCommandLineArgs(string settingName, int settingValue)
        {
            switch (settingName)
            {
                case nameof(Loss):
                {
                    Loss = settingValue;
                    break;
                }
                case nameof(LossMinPktSize):
                {
                    LossMinPktSize = settingValue;
                    break;
                }
                case nameof(LossMaxPktSize):
                {
                    LossMaxPktSize = settingValue;
                    break;
                }
                case nameof(Disorder):
                {
                    Disorder = settingValue > 0;
                    break;
                }
                case nameof(Duplicate):
                {
                    Duplicate = settingValue;
                    break;
                }
                case nameof(Latency):
                {
                    Latency = settingValue;
                    break;
                }
                case nameof(LatencyVariance):
                {
                    LatencyVariance = settingValue;
                    break;
                }
                case nameof(LatencyMin):
                {
                    LatencyMin = settingValue;
                    break;
                }
                case nameof(LatencyMax):
                {
                    LatencyMax = settingValue;
                    break;
                }
                case nameof(Jitter):
                {
                    Jitter = settingValue;
                    break;
                }
                default:
                    return false;
            }
            
            return true;
        }
    }
}