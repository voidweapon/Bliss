namespace Bliss
{
    public class Options : Singleton<Options>
    {
        
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
    }
}