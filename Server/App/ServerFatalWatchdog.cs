using System;
using System.Threading;

namespace Bliss;

/// <summary>Terminates the process if owner-thread fatal cleanup misses its absolute deadline.</summary>
internal static class ServerFatalWatchdog
{
    private const int FATAL_EXIT_CODE = 1;
    private const int WATCHDOG_WAITING_FOR_CLEANUP = 0;
    private const int WATCHDOG_CLEANUP_COMPLETED = 1;
    private const int WATCHDOG_TERMINATION_COMMITTED = 2;

    private static System.Threading.Timer _watchdog;
    private static int _watchdogState;

    internal static void Arm(Exception fatalException)
    {
        // Start the absolute deadline before logging can delay the fatal callback.
        System.Threading.Timer watchdog = new System.Threading.Timer(
            terminateProcess, null, RootManager.FATAL_CLEANUP_DEADLINE_MILLISECONDS, Timeout.Infinite);
        if (Interlocked.CompareExchange(ref _watchdog, watchdog, null) != null)
        {
            watchdog.Dispose();
        }

        Log.Error(fatalException);
    }

    internal static void CompleteCleanup()
    {
        Interlocked.CompareExchange(
            ref _watchdogState, WATCHDOG_CLEANUP_COMPLETED, WATCHDOG_WAITING_FOR_CLEANUP);
        Interlocked.Exchange(ref _watchdog, null)?.Dispose();
    }

    private static void terminateProcess(object state)
    {
        if (Interlocked.CompareExchange(
                ref _watchdogState, WATCHDOG_TERMINATION_COMMITTED, WATCHDOG_WAITING_FOR_CLEANUP)
            != WATCHDOG_WAITING_FOR_CLEANUP)
        {
            return;
        }

        Environment.Exit(FATAL_EXIT_CODE);
    }
}
