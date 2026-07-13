using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Bliss.Base;
using Bliss.Core;
using Bliss.SimpleUI;
using Bliss.Timer;
using ET;
using Unity.UI;
using YooAsset;

namespace Bliss.BootProcess
{
    public class BootProcesser : Singleton<BootProcesser>, ISingletonInit<BootSetting>
    {
        private string _bootPackageName = "BootPackage";

        public void Init(BootSetting bootSetting)
        {
            GlobalDefine.BootSetting = bootSetting;
        }
        
        public async ETTask InitFramework()
        {
            Application.targetFrameRate = GlobalDefine.BootSetting.TargetFPS;
            Application.runInBackground = true;
            var mainCamera = Camera.main;
            mainCamera.enabled = false;
            
            Log.Logger = new UnityLogger();
            ETTask.ExceptionHandler += Log.Error;
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };

            Options options = new Options()
            {
                AppName = "Bliss"
            };
            World.Instance.AddSingleton(options);
            
            TimeInfo.Instance.Init();
            World.Instance.AddSingleton<RootManager>();
            World.Instance.AddSingleton<SimpleResourcesComponent>();


            var boolPackagePalayMode = GetPlayModel(EPlayMode.OfflinePlayMode);
            bool hostBootInitResult = await SimpleResourcesComponent.Instance.CreatePackageAsync(GetPlayModel(GlobalDefine.BootSetting.PlayMode) ,_bootPackageName, false);
            if (!hostBootInitResult)
            {
                // 先销毁旧的Package
                if (YooAssets.ContainsPackage(_bootPackageName))
                {
                    var oldPackage = YooAssets.GetPackage(_bootPackageName);
                    var destroyOldPackage = oldPackage.DestroyAsync();
                    await destroyOldPackage.Task;
                    YooAssets.RemovePackage(_bootPackageName);
                }
                await SimpleResourcesComponent.Instance.CreatePackageAsync(boolPackagePalayMode ,_bootPackageName, false);
            }
            

            World.Instance.AddSingleton<SimpleUIComponent>();
            SimpleUIComponent.Instance.Create<UIBoot>(SimpleUIType.Boot, EUILayer.Low);
            mainCamera.enabled = true;

            var playMode = GetPlayModel(GlobalDefine.BootSetting.PlayMode);
            await SimpleResourcesComponent.Instance.CreatePackageAsync(playMode, "DefaultPackage", true, false);
            
            World.Instance.AddSingleton<CodeLoader>();
            await CodeLoader.Instance.DownloadDllsAsync();
            
            CodeLoader.Instance.Start();
        }

        private static EPlayMode GetPlayModel(EPlayMode playMode)
        {
            if (GlobalDefine.IsEditor)
            {
                var previousModel = playMode;
                if (!GlobalDefine.BootSetting.TestABInEditor)
                {
                    playMode = EPlayMode.EditorSimulateMode;
                }
                Log.Info($"[ResourcesComponent][Editor] Init with play mode: {previousModel} --> {playMode}, TestABInEditor={GlobalDefine.BootSetting.TestABInEditor}  TestDLLInEditor={GlobalDefine.BootSetting.TestDLLInEditor}");
            }

            return playMode;
        }
        
        public async ETTask TrtUpdateBootPackage()
        {
            // 先销毁旧的Package
            if (YooAssets.ContainsPackage(_bootPackageName))
            {
                var oldPackage = YooAssets.GetPackage(_bootPackageName);
                var destroyOldPackage = oldPackage.DestroyAsync();
                await destroyOldPackage.Task;
                YooAssets.RemovePackage(_bootPackageName);
            }
            
            var playMode = GlobalDefine.BootSetting.PlayMode;
            if (GlobalDefine.IsEditor)
            {
                var previousModel = playMode;
                if (!GlobalDefine.BootSetting.TestABInEditor)
                {
                    playMode = EPlayMode.EditorSimulateMode;
                }
            }
            await SimpleResourcesComponent.Instance.CreatePackageAsync(playMode ,_bootPackageName, false, false);
            
        }
        
        [Event(ELogicSceneType.Main)]
        private class EventHandler_BootProcessFinish : AEvent<LogicScene, Event_BootProcessFinish>
        {
            protected override async ETTask Run(LogicScene mainScene, Event_BootProcessFinish eventInfo)
            {
                SimpleUIComponent.Instance.Remove(SimpleUIType.Boot);
                BootProcesser.Instance.TrtUpdateBootPackage().Coroutine();
                await ETTask.CompletedTask;
            }
        }
    }
}