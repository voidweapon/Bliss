namespace Bliss
{
    public class Options : Singleton<Options>
    {
        /// <summary>The default bounded wait for forwarding an internal RPC through Main.</summary>
        public const long DEFAULT_INTERNAL_MESSAGE_FORWARD_TIMEOUT_MS = 40 * 1000;

        
#if DOTNET
        [CommandLine.Option(nameof(AppName), HelpText = "The name of the app.")]
#endif
        public string AppName { get; set; }
        
        
#if DOTNET       
        [CommandLine.Option(nameof(Process), Default = 1, HelpText = "The process be assign.")]
#endif      
        public int Process { get; set; }
 
 
#if DOTNET   
        [CommandLine.Option(nameof(LogLevel), Default = 1, HelpText = "The log level.")]
#endif      
    public int LogLevel { get; set; }
    
    
#if DOTNET      
        [CommandLine.Option(nameof(Console), Default = 1, HelpText = "Is enable console")]
#endif        
        public int Console { get; set; }

#if DOTNET
        [CommandLine.Option(nameof(InternalMessageForwardTimeoutMS),
            Default = DEFAULT_INTERNAL_MESSAGE_FORWARD_TIMEOUT_MS,
            HelpText = "The maximum time in milliseconds that Main waits for a forwarded internal RPC.")]
#endif
        /// <summary>Maximum bounded forwarding wait in milliseconds; must be positive.</summary>
        public long InternalMessageForwardTimeoutMS { get; set; } = DEFAULT_INTERNAL_MESSAGE_FORWARD_TIMEOUT_MS;
    }
}