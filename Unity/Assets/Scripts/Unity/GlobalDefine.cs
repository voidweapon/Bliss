namespace Bliss
{
    public static class GlobalDefine
    {
        /// <summary>
        /// 热更新代码位置
        /// </summary>
        public const string CodeDir = "Assets/Res/Code";
        public const string AOTCodeDir = "Assets/Res/AotCode";
        
#if UNITY_EDITOR
        public static bool IsEditor = true;
#else
        public static bool IsEditor = false;
#endif
        
#if ENABLE_IL2CPP
        public static bool EnableIL2CPP = true;
#else
        public static bool EnableIL2CPP = false;
#endif

        public static BootSetting BootSetting;
    }
}