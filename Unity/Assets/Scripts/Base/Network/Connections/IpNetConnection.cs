namespace Bliss.Core.Network
{
    public abstract class IpNetConnection : ANetConnection
    {
        public EIpNetConnectionType  ConnectionType { get;  protected set; }
    }

    public enum EIpNetConnectionType
    {
        Local,
        Remote,
    }
}