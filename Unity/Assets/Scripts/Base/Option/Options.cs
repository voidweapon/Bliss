namespace Bliss
{
    public class Options
    {
        public static Options Instance;
        
        public int Process { get; set; }
        
        public int Develop { get; set; }
        
        public int LogLevel { get; set; }
        
        public int Console { get; set; }
    }
}