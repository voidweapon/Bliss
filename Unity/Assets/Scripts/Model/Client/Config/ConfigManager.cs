using System.IO;
using Luban.SimpleJSON;

namespace Bliss
{
    [SingletonManger]
    public class ConfigManager : Singleton<ConfigManager>, ISingletonInit
    {
        public const long InvokeType_LoadFile = 1;
        public Tables Tables { get; private set; }
        
        public void Init()
        {
            Tables = new Tables(LoadConfig);
        }
        
        private JSONNode LoadConfig(string fileName)
        {
            var json = EventSystem.Instance.Invoke<LoadFile, string>(InvokeType_LoadFile, new LoadFile() { FileName = $"Assets/Res/Configs/{fileName}.json"});
            return JSON.Parse(json);
        }
        
        public struct LoadFile
        {
            public string FileName;
        }
    }
}