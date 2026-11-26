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

#if DOTNET
            await RootManager.Instance.Create(0, ELogicSceneType.Main, nameof(ELogicSceneType.Main),
                useInitializationGate: true);
            await EventSystem.Instance.Invoke<Invoke_CreateConfiguredServerRoots, ETTask>(
                (long)ELogicSceneType.Main, new Invoke_CreateConfiguredServerRoots());
            Log.Info("==============Server Init Success==============");
#else
            await RootManager.Instance.Create(0, ELogicSceneType.Main, nameof(ELogicSceneType.Main));
#endif
        }
    }

    public struct Event_InitProcessEntry
    {
        
    }
    
    public struct Event_InitProcessEntry_Demo
    {
        
    }

#if DOTNET
    /// <summary>Requests initialization of the configured Roots after Main is ready.</summary>
    public struct Invoke_CreateConfiguredServerRoots
    {
    }
#endif
}