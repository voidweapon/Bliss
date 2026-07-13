using Bliss.Core;
using MemoryPack;

namespace Bliss
{
    using SerializeInclude = MemoryPackIncludeAttribute;
    
    public enum ENetMode
    {
        /** Standalone: a game without networking, with one or more local players. Still considered a server because it has all server functionality. */
        NM_Standalone,

        /** Dedicated server: server with no local players. */
        NM_DedicatedServer,

        /** Listen server: a server that also has a local player who is hosting the game, available to other players on the network. */
        NM_ListenServer,

        /**
         * Network client: client connected to a remote server.
         * Note that every mode less than this value is a kind of server, so checking NetMode < NM_Client is always some variety of server.
         */
        NM_Client,

        NM_MAX,
    }
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