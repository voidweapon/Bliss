using System.Net;

namespace Bliss
{
    public sealed partial class ServerProcess
    {
        /// <summary>
        /// 内网Ip
        /// </summary>
        public string InnerIp => MachineCfg.InnerIp;
        
        /// <summary>
        /// 外网IP
        /// </summary>
        public string OuterIp => MachineCfg.OuterIp;
        
        private IPEndPoint _innerIpInnerPort;

        /// <summary>
        /// 同一台机器上的进程之间通信使用内网IP和端口
        /// </summary>
        public IPEndPoint InnerIpInnerPort
        {
            get
            {
                if (_innerIpInnerPort == null)
                {
                    _innerIpInnerPort = NetworkHelper.ToIPEndPoint($"{this.InnerIp}:{this.Port}");
                }

                return _innerIpInnerPort;
            }
        }
        
        public ServerMachine MachineCfg => ConfigManager.Instance.Tables.TbServerMachine.Get(this.MachineId);

    }
}
