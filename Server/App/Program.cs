using System;
using System.Threading;
using CommandLine;
using ET;

namespace Bliss
{
    public static class Program
    {
        public static void Main()
        {
            RootManager rootManager = null;
            bool fatalLifecycleConfigured = false;
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Log.Error(e.ExceptionObject.ToString());
                };
            
                Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs())
                    .WithNotParsed(error => Log.Error($"{error}"))
                    .WithParsed(options => World.Instance.AddSingleton(options));

                Log.Logger = new NLogger(Options.Instance.AppName, Options.Instance.Process, 0);
                Log.LogLevel = Options.Instance.LogLevel;
                ETTask.ExceptionHandler += Log.Error;
                Log.Debug($"Use Logger:{Log.Logger.GetType()}"); 

                TimeInfo.Instance.Init();
                rootManager = World.Instance.AddSingleton<RootManager>();
                rootManager.ConfigureFatalLifecycle(ServerFatalWatchdog.Arm, ServerFatalWatchdog.CompleteCleanup);
                fatalLifecycleConfigured = true;
                World.Instance.AddSingleton<CodeLoader>();
            }
            catch (Exception exception)
            {
                if (rootManager == null || !fatalLifecycleConfigured)
                {
                    Log.Error(exception);
                    Environment.ExitCode = 1;
                    return;
                }

                rootManager.ReportFatal(exception);
            }

            while (true)
            {
                Thread.Sleep(1);
                try
                {
                    World.Instance.EarlyUpdate();
                    World.Instance.Update();
                    World.Instance.LateUpdate();
                }
                catch (Exception e)
                {
                    rootManager.ReportFatal(e);
                }

                if (rootManager.FatalCleanupCompleted)
                {
                    Environment.ExitCode = 1;
                    return;
                }
            }
        }
    }
}