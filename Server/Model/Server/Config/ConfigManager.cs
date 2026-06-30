using System.IO;
using System.Text.Json;

namespace Bliss
{
    [SingletonManger]
    public class ConfigManager : Singleton<ConfigManager>, ISingletonInit
    {
        public Tables Tables { get; private set; }
        
        public void Init()
        {
            Tables = new Tables(LoadConfig);
            Tables.EndInit();
        }
        
        private JsonElement LoadConfig(string fileName)
        {
            var json = File.ReadAllText($"../ServerRes/Configs/{fileName}.json");
            var document = JsonDocument.Parse(json);
            return document.RootElement;
        }
    }
}

