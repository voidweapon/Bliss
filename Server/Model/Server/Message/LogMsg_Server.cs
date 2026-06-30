using System.Collections.Generic;

namespace Bliss
{
    [SingletonManger]
    public class LogMsg_ServerRule : Singleton<LogMsg_ServerRule>, ISingletonInit
    {
        private readonly HashSet<ushort> _include = new HashSet<ushort>()
        {
            InnerMessageOpcode.G2M_PlayerOfflineMessage,
        };
        
        public void Init()
        {
            LogMsg.Instance.AddIncludeRule(_include);
        }
    }
}