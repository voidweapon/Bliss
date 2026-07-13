using Bliss.Core;
using ET;

namespace Bliss
{
    public static class Entry
    {
        public static void Start()
        {
            Log.Info($"[HybridCLR]Loading {nameof(Entry)}  {typeof(Entry).Assembly.Location}");
            StartAsync().Coroutine();
        }

        private static async ETTask StartAsync()
        {
#if DOTNET
            Bliss.Server.MongoRegister.Init();
#endif
            MemoryPackSerializerRegister.Init();
            World.Instance.AddSingleton<MessageOpcodeManager>();
            World.Instance.AddSingleton<LogMsg>();
            World.Instance.AddSingleton<GameNetworkManager>();
            
            //创建支持reload的 singleton
            TypeManager.Instance.CreateAutoSingletonManager();

            await RootManager.Instance.Create(0, ELogicSceneType.Main, nameof(ELogicSceneType.Main));
        }
    }

    public struct Event_InitProcessEntry
    {
        
    }
    
    public struct Event_InitProcessEntry_Demo
    {
        
    }
}