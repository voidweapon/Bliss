using System;
using System.Diagnostics;

namespace Bliss
{
    public static class Log
    {
        private const int TraceLevel = 1;
        private const int DebugLevel = 2;
        private const int InfoLevel = 3;
        private const int WarningLevel = 4;
        
        public static ILogger Logger { get; set; }
        public static int LogLevel { get; set; } = 1;
        
        private static ILogger GetLog()
        {
            return Logger;
        }

        
        
        private static int GetLogLevel()
        {
            return LogLevel;            
        }
        
        private static int EnableLogConsole()
        {
            return 0;
        }
        
        [Conditional("DEBUG")]
        public static void Debug(string msg)
        {
            if (GetLogLevel() > DebugLevel)
            {
                return;
            }

            GetLog().Debug(msg);
        }
        
        [Conditional("DEBUG")]
        public static void Trace(string msg)
        {
            if (GetLogLevel() > TraceLevel)
            {
                return;
            }
            StackTrace st = new(1, true);
            GetLog().Trace($"{msg}\n{st}");
        }

        public static void Info(string msg)
        {
            if (GetLogLevel() > InfoLevel)
            {
                return;
            }
            GetLog().Info(msg);
        }

        public static void TraceInfo(string msg)
        {
            if (GetLogLevel() > InfoLevel)
            {
                return;
            }
            StackTrace st = new(1, true);
            GetLog().Trace($"{msg}\n{st}");
        }

        public static void Warning(string msg)
        {
            if (GetLogLevel() > WarningLevel)
            {
                return;
            }
            GetLog().Warning(msg);
        }

        public static void Error(string msg)
        {
            StackTrace st = new(1, true);
            GetLog().Error($"{msg}\n{st}");
        }

        public static void Error(Exception e)
        {
            GetLog().Error(e.ToString());
        }
        
        public static void Console(string msg)
        {
            if (EnableLogConsole() == 1)
            {
                System.Console.WriteLine(msg);
            }
            GetLog().Debug(msg);
        }
    }
}