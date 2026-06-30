using Bliss.Core;
using MemoryPack;

namespace Bliss
{
    using SerializeInclude = MemoryPackIncludeAttribute;
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
        private static void Awake(this NetRoleComponent self, NetRoleComponentInitParam param1)
        {
            self.LocalNetRole = param1.LocalNetRole;;
            self.RemoteNetRole = param1.RemoteNetRole;;
        }
    }

    [MemoryPackable(GenerateType.Object)]
    public partial class NetRoleComponent : Entity, IAwake<NetRoleComponentInitParam>, ISerializableEntity
    {
        [SerializeInclude]
        public ENetRole LocalNetRole;
        [SerializeInclude]
        public ENetRole RemoteNetRole;
    }

    public struct NetRoleComponentInitParam
    {
        public ENetRole LocalNetRole;
        public ENetRole RemoteNetRole;
    }
}