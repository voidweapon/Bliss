using System;
using System.Collections;
using System.Collections.Generic;
using Bliss;
using Bliss.Base.Event;
using ET;
using UnityEngine;
using YooAsset;

namespace Bliss
{
    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    public class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }

        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }

        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }

    public sealed class SimpleResourcesComponent : Singleton<SimpleResourcesComponent>, ISingletonInit
    {
        public AutoEventAction<WeakReference<DownloaderOperation>> OnDownloadEvent = new AutoEventAction<WeakReference<DownloaderOperation>>();
        public AutoEventAction<WeakReference<ClearCacheFilesOperation>> OnClearCacheFileEvent = new AutoEventAction<WeakReference<ClearCacheFilesOperation>>();
        public AutoEventAction<WeakReference<AsyncOperationBase>> OnErrorEvent = new AutoEventAction<WeakReference<AsyncOperationBase>>();
        

        public void Init()
        {
            YooAssets.Initialize();
        }

        protected override void Destroy()
        {
            YooAssets.Destroy();
            OnDownloadEvent.Dispose();
            OnClearCacheFileEvent.Dispose();
            OnErrorEvent.Dispose();
        }
        
        public async ETTask<bool> CreatePackageAsync(EPlayMode playMode, string packageName, bool isDefault = false, bool haveBuildInFiles = true)
        {
            ResourcePackage package = YooAssets.CreatePackage(packageName);
            if (isDefault)
            {
                YooAssets.SetDefaultPackage(package);
            }

            InitializationOperation initOperation = null;
            switch (playMode)
            {
                case EPlayMode.EditorSimulateMode:
                {
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
                    var packageRoot = buildResult.PackageRootDirectory;
                    var fileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);

                    var createParameters = new EditorSimulateModeParameters();
                    createParameters.EditorFileSystemParameters = fileSystemParams;
                    initOperation = package.InitializeAsync(createParameters);
                    await initOperation.Task;
                    break;
                }
                case EPlayMode.OfflinePlayMode:
                {
                    var fileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

                    var createParameters = new OfflinePlayModeParameters();
                    createParameters.BuildinFileSystemParameters = fileSystemParams;

                    initOperation = package.InitializeAsync(createParameters);
                    await initOperation.Task;
                    break;
                }
                case EPlayMode.HostPlayMode:
                {
                    string defaultHostServer = GetHostServerURL(GlobalDefine.BootSetting.ResDefaultHostServer, GlobalDefine.BootSetting.AppVersion,  packageName);
                    string fallbackHostServer = GetHostServerURL(GlobalDefine.BootSetting.ResFallbackHostServer, GlobalDefine.BootSetting.AppVersion, packageName);
                     Log.Info($"[ResourcesComponent][{packageName}] RemoteService defaults={defaultHostServer}, Fallback={fallbackHostServer}");
                    IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                    var cacheFileSystemParams = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);

                    FileSystemParameters buildinFileSystemParams = null;
                    if (haveBuildInFiles)
                    { 
                        buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    }

                    var createParameters = new HostPlayModeParameters();
                    createParameters.BuildinFileSystemParameters = buildinFileSystemParams;
                    createParameters.CacheFileSystemParameters = cacheFileSystemParams;

                    initOperation = package.InitializeAsync(createParameters);
                    await initOperation.Task;
                    break;
                }
                case EPlayMode.WebPlayMode:
                {
                    string defaultHostServer = GetHostServerURL(GlobalDefine.BootSetting.ResDefaultHostServer, GlobalDefine.BootSetting.AppVersion, packageName);
                    string fallbackHostServer = GetHostServerURL(GlobalDefine.BootSetting.ResFallbackHostServer,  GlobalDefine.BootSetting.AppVersion,  packageName);
                    IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                    var webServerFileSystemParams = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                    var webRemoteFileSystemParams =
                        FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices); //支持跨域下载

                    var createParameters = new WebPlayModeParameters();
                    createParameters.WebServerFileSystemParameters = webServerFileSystemParams;
                    createParameters.WebRemoteFileSystemParameters = webRemoteFileSystemParams;

                    initOperation = package.InitializeAsync(createParameters);
                    await initOperation.Task;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (initOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log("资源包初始化成功！");
            }
            else
            {
                Debug.LogError($"资源包初始化失败：{initOperation.Error}");
                OnErrorEvent.Invoke(new WeakReference<AsyncOperationBase>(initOperation));
                return false;
            }

            var requestOperation =  package.RequestPackageVersionAsync();
            await requestOperation.Task;
            if (requestOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"Request package version : {requestOperation.PackageVersion}");
            }
            else
            {
                Debug.LogError($"Request package version fail : {requestOperation.Error}");
                OnErrorEvent.Invoke(new WeakReference<AsyncOperationBase>(requestOperation));
                return false;
            }

            var updatePackageManifestOperation = package.UpdatePackageManifestAsync(requestOperation.PackageVersion);
            await updatePackageManifestOperation.Task;
            if (updatePackageManifestOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"update package manifest : success");
            }
            else
            {
                Debug.LogError($"update package manifest fail : {updatePackageManifestOperation.Error}");
                OnErrorEvent.Invoke(new WeakReference<AsyncOperationBase>(updatePackageManifestOperation));
                return false;
            }

            var downloader = package.CreateResourceDownloader(10, 3);
            if (downloader.TotalDownloadCount == 0)
            {
                //不需要更新
                return true;
            }

            // 发现新更新文件后走更新流程
            downloader.BeginDownload();
            await downloader.Task;

            if (downloader.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"update package  fail : {downloader.Error}");
                OnErrorEvent.Invoke(new WeakReference<AsyncOperationBase>(downloader)); 
                return false;
            }

            //清理未使用的Bundle
            var clearOp = package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            await clearOp.Task;
            if (clearOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"clear operation fail : {clearOp.Error}");
                OnErrorEvent.Invoke(new WeakReference<AsyncOperationBase>(clearOp));
                return false;
            }
            return true;
        }
        
        public void DestroyPackage(string packageName)
        {
            ResourcePackage package = YooAssets.GetPackage(packageName);
            var operation = package.UnloadUnusedAssetsAsync();
            operation.WaitForAsyncComplete();
        }
        
        public T LoadAsset<T>(string location) where T : UnityEngine.Object
        {
            AssetHandle handle = YooAssets.LoadAssetSync<T>(location);
            T t = (T)handle.AssetObject;
            handle.Release();
            return t;
        }
        
        public T LoadAsset<T>(string packageName, string location) where T : UnityEngine.Object
        {
            var package = YooAssets.GetPackage(packageName);
            AssetHandle handle = package.LoadAssetSync<T>(location);
            T t = (T)handle.AssetObject;
            handle.Release();
            return t;
        }

        /// <summary>
        /// 加载制定路径的资源
        /// 主要用来加载dll config aotdll.
        /// <param name="location">资源的路径</param>
        /// </summary>
        public async ETTask<T> LoadAssetAsync<T>(string location) where T : UnityEngine.Object
        {
            AssetHandle handle = YooAssets.LoadAssetAsync<T>(location);
            await handle.Task;
            T t = (T)handle.AssetObject;
            handle.Release();
            return t;
        }

        /// <summary>
        /// 加载包中所有资源
        /// 主要用来加载dll config aotdll
        /// <param name="location">包中任意资源的路径</param>
        /// </summary>
        public async ETTask<Dictionary<string, T>> LoadAllAssetsAsync<T>(string location) where T : UnityEngine.Object
        {
            AllAssetsHandle allAssetsOperationHandle = YooAssets.LoadAllAssetsAsync<T>(location);
            await allAssetsOperationHandle.Task;
            Dictionary<string, T> dictionary = new Dictionary<string, T>();
            foreach (UnityEngine.Object assetObj in allAssetsOperationHandle.AllAssetObjects)
            {
                T t = assetObj as T;
                dictionary.Add(t.name, t);
            }

            allAssetsOperationHandle.Release();
            return dictionary;
        }

        static string GetHostServerURL(string hostServer, string appVersion, string packageName)
        {
            //string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
            
#if UNITY_EDITOR
            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
            {
                return $"{hostServer}/CDN/Android/{appVersion}/{packageName}";
            }
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
            {
                return $"{hostServer}/CDN/IPhone/{appVersion}/{packageName}";
            }
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
            {
                return $"{hostServer}/CDN/WebGL/{appVersion}/{packageName}";
            }
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneOSX)
            {
                return $"{hostServer}/CDN/StandaloneOSX/{appVersion}/{packageName}";
            }
            return $"{hostServer}/CDN/PC/{appVersion}/{packageName}";
#else
            if (Application.platform == RuntimePlatform.Android)
            {
                return $"{hostServer}/CDN/Android/{appVersion}/{packageName}";
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return $"{hostServer}/CDN/IPhone/{appVersion}/{packageName}";
            }
            else if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                return $"{hostServer}/CDN/WebGL/{appVersion}/{packageName}";
            }
            else if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                return $"{hostServer}/CDN/StandaloneOSX/{appVersion}/{packageName}";
            }

            return $"{hostServer}/CDN/PC/{appVersion}/{packageName}";
#endif
        }
    }
}