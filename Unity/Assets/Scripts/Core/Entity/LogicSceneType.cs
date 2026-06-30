using System;

namespace Bliss
{
    [Flags]
    public enum ELogicSceneType : long
    {
        None = 0L,
        
        Main = 1L << 0,
        //服务器
        Realm = 1L << 1,
        Gate = 1L << 2,
        Location = 1 << 3,
        Map = 1 << 4,
        
        GameplayWorld = 1L << 5,
        //GamePlay的GameplayWorld下的一个Level
        Level = 1 << 6,
        
        
        //客户端
        Client = 1L << 32,
    }

    public static class ELogicSceneTypeHelper
    {
        public static bool HasSameFlag(this ELogicSceneType a, ELogicSceneType b)
        {
            if (((ulong) a & (ulong) b) == 0)
            {
                return false;
            }
            return true;
        }
    }
}