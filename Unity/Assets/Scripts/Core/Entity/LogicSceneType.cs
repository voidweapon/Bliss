using System;

namespace Bliss
{
    [Flags]
    public enum ELogicSceneType : long
    {
        None = 0,
        
        Main = 1 << 0,
    }
}