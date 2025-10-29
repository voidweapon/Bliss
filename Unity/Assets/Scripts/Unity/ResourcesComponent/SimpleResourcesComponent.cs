using System;
using System.Collections;
using System.Collections.Generic;
using Bliss;
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

    public sealed class SimpleResourcesComponent : Singleton<SimpleResourcesComponent>, ISingletonInit<EPlayMode>
    {
        private EPlayMode _playMode;

        public void Init(EPlayMode playMode)
        {
            YooAssets.Initialize();
            _playMode = playMode;
        }

        protected override void Destroy()
        {
            YooAssets.Destroy();
        }

        public async ETTask CreatePackageAsync(string packageName, bool isDefault = false)
        {
            ResourcePackage package = YooAssets.CreatePackage(packageName);
            if (isDefault)
            {
                YooAssets.SetDefaultPackage(package);
            }

            InitializationOperation initOperation = null;
            switch (_playMode)
            {
                case EPlayMode.EditorSimulateMode:
                {
                    var buildResult = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
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
                    string defaultHostServer = "http://127.0.0.1/CDN/Android/v1.0";
                    string fallbackHostServer = "http://127.0.0.1/CDN/Android/v1.0";
                    IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                    var cacheFileSystemParams =
                        FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                    var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

                    var createParameters = new HostPlayModeParameters();
                    createParameters.BuildinFileSystemParameters = buildinFileSystemParams;
                    createParameters.CacheFileSystemParameters = cacheFileSystemParams;

                    initOperation = package.InitializeAsync(createParameters);
                    await initOperation.Task;
                    break;
                }
                case EPlayMode.WebPlayMode:
                {
                    string defaultHostServer = "http://127.0.0.1/CDN/Android/v1.0";
                    string fallbackHostServer = "http://127.0.0.1/CDN/Android/v1.0";
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
            }

            var updatePackageManifestOperation = package.UpdatePackageManifestAsync(requestOperation.PackageVersion);
            await updatePackageManifestOperation.Task;
            if (updatePackageManifestOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"update package manifest : success");
            }
            else
            {
                Debug.LogError($"update package manifest fail : {requestOperation.Error}");
            }

            var downloader = package.CreateResourceDownloader(10, 3);
            if (downloader.TotalDownloadCount == 0)
            {
                //不需要更新
                return;
            }

            // 发现新更新文件后走更新流程
            downloader.BeginDownload();
            await downloader.Task;

            if (downloader.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"update package  fail : {downloader.Error}");
            }

            //清理未使用的Bundle
            var clearOp = package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            await clearOp.Task;
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

        static string GetHostServerURL()
        {
            //string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
            string hostServerIP = "http://127.0.0.1";
            string appVersion = "v1.0";

#if UNITY_EDITOR
            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
            {
                return $"{hostServerIP}/CDN/Android/{appVersion}";
            }
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
            {
                return $"{hostServerIP}/CDN/IPhone/{appVersion}";
            }
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
            {
                return $"{hostServerIP}/CDN/WebGL/{appVersion}";
            }

            return $"{hostServerIP}/CDN/PC/{appVersion}";
#else
            if (Application.platform == RuntimePlatform.Android)
            {
                return $"{hostServerIP}/CDN/Android/{appVersion}";
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return $"{hostServerIP}/CDN/IPhone/{appVersion}";
            }
            else if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                return $"{hostServerIP}/CDN/WebGL/{appVersion}";
            }

            return $"{hostServerIP}/CDN/PC/{appVersion}";
#endif
        }
    }
}