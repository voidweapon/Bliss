using System;
using System.Diagnostics;

namespace Bliss.Base
{
    public static class Assert
    {
        [DebuggerHidden]
        public static void Check(bool condition)
        {
            if (!condition)
            {
                throw new Exception();
            }
        }
        
        [DebuggerHidden]
        public static bool Ensure(bool condition)
        {
            if (!condition)
            {
                Log.Debug(new StackTrace().ToString());
            }
            return condition;
        }

        [DebuggerHidden]
        public static bool EnsureMsg(bool condition, string msg)
        {
            if (!condition)
            {
                Log.Debug($"{msg}\n{new StackTrace()}");
            }
            return condition;
        }
    }
}