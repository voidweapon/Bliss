namespace Bliss.Core.Network
{
    public class PrimarySecondaryIpNetConnection : IpNetConnection
    {
        /// <summary>
        /// 主连接
        /// </summary>
        private IpNetConnection _primaryConnection;
        
        /// <summary>
        /// 主连接不可用时的备用连接
        /// </summary>
        private IpNetConnection _secondaryConnection;
        
    }
}