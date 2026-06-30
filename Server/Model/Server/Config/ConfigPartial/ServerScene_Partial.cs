using System;
using System.Net;

namespace Bliss
{
    public sealed partial class ServerScene
    {
        private IPEndPoint _innerIpOuterPort;
        /// <summary>
        /// 内网地址外网端口
        /// </summary>
        public IPEndPoint InnerIpOuterPort
        {
            get
            {
                if (_innerIpOuterPort == null)
                {
                    _innerIpOuterPort = NetworkHelper.ToIPEndPoint($"{this.ProcessCfg.InnerIp}:{this.OuterPort}");
                }
                return _innerIpOuterPort;
            }
        }
        

        private IPEndPoint _outerIpOuterPort;
        /// <summary>
        /// 外网地址外网端口
        /// </summary>
        public IPEndPoint OuterIpOuterPort
        {
            get
            {
                if (_outerIpOuterPort == null)
                {
                    _outerIpOuterPort = NetworkHelper.ToIPEndPoint($"{this.ProcessCfg.OuterIp}:{this.OuterPort}");
                }
                return _outerIpOuterPort;
            }
        }
        
        public ServerProcess ProcessCfg => ConfigManager.Instance.Tables.TbServerProcess.Get(this.Process);


        public ELogicSceneType LogicSceneType { get; private set; }
        
        public ActorAddress ActorAddress { get; private set; }

        public void EndInit()
        {
            ActorAddress = new ActorAddress(this.Process, Id);
            LogicSceneType = Enum.Parse<ELogicSceneType>(SceneType);
        }
    }
}

