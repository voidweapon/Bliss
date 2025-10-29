using System;

namespace Bliss
{
    [Flags]
    public enum ELogicSceneType : long
    {
        None = 0,
        
        Main = 1 << 0,
        
        Client = 1 << 0,
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