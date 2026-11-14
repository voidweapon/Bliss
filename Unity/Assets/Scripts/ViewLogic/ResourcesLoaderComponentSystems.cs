using System;
using System.Collections.Generic;
using Bliss.Core;
using Bliss.CoroutineLocks;
using Bliss.ViewModel;
using ET;
using UnityEngine.SceneManagement;
using YooAsset;

namespace Bliss.ViewLogic
{
    [EntitySystemOf(typeof(ResourcesLoaderComponent))]
    public static partial class ResourcesLoaderComponentSystems
    {
        private static void Awake(this ResourcesLoaderComponent self)
        {
            self.package = YooAssets.GetPackage("DefaultPackage");
        }

        private static void Awake(this ResourcesLoaderComponent self, string packageName)
        {
            self.package = YooAssets.GetPackage(packageName);
        }

        private static void Destroy(this ResourcesLoaderComponent self)
        {
            foreach (var kv in self.handlers)
            {
                switch (kv.Value)
                {
                    case AssetHandle handle:
                        handle.Release();
                        break;
                    case AllAssetsHandle handle:
                        handle.Release();
                        break;
                    case SubAssetsHandle handle:
                        handle.Release();
                        break;
                    case RawFileHandle handle:
                        handle.Release();
                        break;
                    case SceneHandle handle:
                        handle.UnloadAsync();
                        break;
                }
            }
        }

        public static bool CheckLocationValid(this ResourcesLoaderComponent self, string location)
        {
            return self.package.CheckLocationValid(location);
        }
        
        public static T LoadAsset<T>(this ResourcesLoaderComponent self, string location) where T : UnityEngine.Object
        {
            if (!self.handlers.TryGetValue(location, out var handler))
            {
                handler = self.package.LoadAssetSync<T>(location);
                self.handlers.Add(location, handler);
            }
            return (T)((AssetHandle)handler).AssetObject;
        }
        
        public static async ETTask<T> LoadAssetAsync<T>(this ResourcesLoaderComponent self, string location) where T : UnityEngine.Object
        {
            long instance = self.InstanceId;
            using CoroutineLock coroutineLock = await self.RootScene().GetComponent<CoroutineLockComponent>().WaitAsync(ECoroutineLockType.ResourcesLoader, location.GetHashCode());
            if (instance != self.InstanceId)
            {
                return null;
            }
            
            HandleBase handler;
            if (!self.handlers.TryGetValue(location, out handler))
            {
                handler = self.package.LoadAssetAsync<T>(location);
                await handler.Task;
                
                if (instance != self.InstanceId)
                {
                    handler.Release();
                    return null;
                }

                self.handlers.Add(location, handler);
            }
            return (T)((AssetHandle)handler).AssetObject;
        }
        
        public static async ETTask<Dictionary<string, T>> LoadAllAssetsAsync<T>(this ResourcesLoaderComponent self, string location) where T : UnityEngine.Object
        {
            long instance = self.InstanceId;
            using CoroutineLock coroutineLock = await self.RootScene().GetComponent<CoroutineLockComponent>().WaitAsync(ECoroutineLockType.ResourcesLoader, location.GetHashCode());
            if (instance != self.InstanceId)
            {
                return null;
            }

            HandleBase handler;
            if (!self.handlers.TryGetValue(location, out handler))
            {
                handler = self.package.LoadAllAssetsAsync<T>(location);
                await handler.Task;
                
                if (instance != self.InstanceId)
                {
                    handler.Release();
                    return null;
                }
                
                self.handlers.Add(location, handler);
            }

            Dictionary<string, T> dictionary = new Dictionary<string, T>();
            foreach (UnityEngine.Object assetObj in ((AllAssetsHandle)handler).AllAssetObjects)
            {
                T t = assetObj as T;
                dictionary.Add(t.name, t);
            }

            return dictionary;
        }

        public static async ETTask<SceneHandle> LoadSceneAsync(this ResourcesLoaderComponent self, string location, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            long instance = self.InstanceId;
            using CoroutineLock coroutineLock = await self.RootScene().GetComponent<CoroutineLockComponent>().WaitAsync(ECoroutineLockType.ResourcesLoader, location.GetHashCode());
            if (instance != self.InstanceId)
            {
                return null;
            }
            
            HandleBase handler;
            if (self.handlers.TryGetValue(location, out handler))
            {
                return (SceneHandle)handler;
            }

            handler = self.package.LoadSceneAsync(location, loadSceneMode);

            await handler.Task;
            
            if (instance != self.InstanceId)
            {
                await ((SceneHandle)handler).UnloadAsync().Task;
                handler.Release();
                return null;
            }
            
            self.handlers.Add(location, handler);
            return (SceneHandle)handler;
        }

        /// <summary>
        /// Unloads a scene owned by this resource loader and removes its handle from the loader.
        /// </summary>
        /// <param name="self">The resource loader that owns the scene handle.</param>
        /// <param name="scene">The loaded scene to unload.</param>
        /// <exception cref="InvalidOperationException">The scene is not owned by this resource loader.</exception>
        public static async ETTask UnloadSceneAsync(this ResourcesLoaderComponent self, Scene scene)
        {
            string location = null;
            SceneHandle sceneHandle = null;
            foreach (KeyValuePair<string, HandleBase> entry in self.handlers)
            {
                if (entry.Value is SceneHandle candidate && candidate.SceneObject == scene)
                {
                    location = entry.Key;
                    sceneHandle = candidate;
                    break;
                }
            }

            if (sceneHandle == null)
            {
                throw new InvalidOperationException(
                    $"Scene {scene.name} is not owned by {nameof(ResourcesLoaderComponent)}.");
            }

            await sceneHandle.UnloadAsync().Task;
            self.handlers.Remove(location);
        }
    }
}