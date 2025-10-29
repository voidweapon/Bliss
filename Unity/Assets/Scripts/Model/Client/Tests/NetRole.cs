using Bliss.Core;

namespace Bliss
{
    public enum ENetRole
    {
        None = 0,
        /// <summary>
        /// 模拟代理
        /// </summary>
        SimulatedProxy,
        /// <summary>
        /// 自治代理
        /// </summary>
        AutonomousProxy,
        /// <summary>
        /// 权威
        /// </summary>
        Authority,
    }

    [EntitySystemOf(typeof(NetRoleComponent))]
    public static partial class NetRoleComponentSystems
    {
        public static void Awake(this NetRoleComponent self, NetRoleComponentInitParam param1)
        {
            self.LocalNetRole = param1.LocalNetRole;;
            self.RemoteNetRole = param1.RemoteNetRole;;
        }
    }

    public class NetRoleComponent : Entity, IAwake<NetRoleComponentInitParam>
    {
        public ENetRole LocalNetRole;
        public ENetRole RemoteNetRole;
    }

    public struct NetRoleComponentInitParam
    {
        public ENetRole LocalNetRole;
        public ENetRole RemoteNetRole;
    }
}