using Bliss.Core;
using ET;

namespace Bliss
{
    public static class Entry
    {
        public static void Start()
        {
            StartAsync().Coroutine();
        }

        private static async ETTask StartAsync()
        {
#if DOTNET
            Bliss.Server.MongoRegister.Init();
#endif
            MemoryPackSerializerRegister.Init();
            World.Instance.AddSingleton<MessageOpcodeManager>();
            
            //创建支持reload的 singleton
            TypeManager.Instance.CreateAutoSingletonManager();

            await RootManager.Instance.Create(0, ELogicSceneType.Main, nameof(ELogicSceneType.Main));
        }
    }
}