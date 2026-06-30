using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace Bliss.BuildTools
{
    public static class BuildYooAsset
    {
        public static BuildResult BuildInternal(BuildTarget buildTarget, string packageName, string packageVersion)
        {
            Debug.Log($"开始构建 : {buildTarget}");
            
            if (string.IsNullOrEmpty(packageVersion))
            {
                packageVersion = GetDefaultPackageVersion();
                Debug.Log($"{nameof(packageVersion)} is null or empty use default {nameof(packageVersion)} {packageVersion}");
            }
            
            string pipelineName = EBuildPipeline.ScriptableBuildPipeline.ToString();
            var fileNameStyle = AssetBundleBuilderSetting.GetPackageFileNameStyle(packageName, pipelineName);
            var buildinFileCopyOption = AssetBundleBuilderSetting.GetPackageBuildinFileCopyOption(packageName, pipelineName);
            var buildinFileCopyParams = AssetBundleBuilderSetting.GetPackageBuildinFileCopyParams(packageName, pipelineName);
            var compressOption = AssetBundleBuilderSetting.GetPackageCompressOption(packageName, pipelineName);
            var clearBuildCache = AssetBundleBuilderSetting.GetPackageClearBuildCache(packageName, pipelineName);
            var useAssetDependencyDB = AssetBundleBuilderSetting.GetPackageUseAssetDependencyDB(packageName, pipelineName);
            var builtinShaderBundleName = GetBuiltinShaderBundleName(packageName);
            
            // 构建参数
            ScriptableBuildParameters buildParameters = new ScriptableBuildParameters();
            buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();;
            buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();;
            buildParameters.BuildPipeline = pipelineName;
            buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle; //必须指定资源包类型
            buildParameters.BuildTarget = buildTarget;
            buildParameters.PackageName = packageName;
            buildParameters.PackageVersion = packageVersion;
            buildParameters.EnableSharePackRule = true; //启用共享资源构建模式，兼容1.5x版本
            buildParameters.VerifyBuildingResult = true;
            buildParameters.FileNameStyle = fileNameStyle;
            buildParameters.BuildinFileCopyOption = buildinFileCopyOption;
            buildParameters.BuildinFileCopyParams = buildinFileCopyParams;
            buildParameters.CompressOption = compressOption;
            buildParameters.ClearBuildCacheFiles = clearBuildCache; //不清理构建缓存，启用增量构建，可以提高打包速度！
            buildParameters.UseAssetDependencyDB = useAssetDependencyDB; //使用资源依赖关系数据库，可以提高打包速度！
            buildParameters.BuiltinShadersBundleName = builtinShaderBundleName;
            buildParameters.EncryptionServices = CreateEncryptionServicesInstance(packageName, pipelineName);
            buildParameters.ManifestProcessServices = CreateManifestProcessServicesInstance(packageName, pipelineName);
            buildParameters.ManifestRestoreServices = CreateManifestRestoreServicesInstance(packageName, pipelineName);
    
            // 执行构建
            ScriptableBuildPipeline pipeline = new ScriptableBuildPipeline();
            var buildResult = pipeline.Run(buildParameters, true);
            if (buildResult.Success)
            {
                Debug.Log($"构建成功 : {buildResult.OutputPackageDirectory}");
            }
            else
            {
                Debug.LogError($"构建失败 : {buildResult.ErrorInfo}");
            }
            
            return buildResult;
        }

        public static void CopyToFileServer(string packageDirectory, BuildTarget buildTarget, string appVersion, string packageName)
        {
            var toDir = Path.GetFullPath($"{BuildDefine.FileServerDir}/{buildTarget}/{appVersion}/{packageName}", Application.dataPath);
            if (!Directory.Exists(toDir))
            {
                Directory.CreateDirectory(toDir);
            }
            FileHelper.CopyDirectory(packageDirectory, toDir);
            Debug.Log($"拷贝资源包到文件服务器成功 : {packageDirectory} ->  {toDir}");
        }
        
        /// <summary>
        /// 内置着色器资源包名称
        /// 注意：和自动收集的着色器资源包名保持一致！
        /// </summary>
        static string GetBuiltinShaderBundleName(string packageName)
        {
            var uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
            var packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
            return packRuleResult.GetBundleName(packageName, uniqueBundleName);
        }
        
        /// <summary>
        /// 获取默认版本
        /// </summary>
        static string GetDefaultPackageVersion()
        {
            int totalMinutes = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            return DateTime.Now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
        }

        /// <summary>
        /// 创建资源包加密服务类实例
        /// </summary>
        static IEncryptionServices CreateEncryptionServicesInstance(string packageName, string pipelineName)
        {
            var className = AssetBundleBuilderSetting.GetPackageEncyptionServicesClassName(packageName, pipelineName);
            var classTypes = EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
            var classType = classTypes.Find(x => x.FullName.Equals(className));
            if (classType != null)
                return (IEncryptionServices)Activator.CreateInstance(classType);
            else
                return null;
        }

        /// <summary>
        /// 创建资源清单加密服务类实例
        /// </summary>
        static IManifestProcessServices CreateManifestProcessServicesInstance(string packageName, string pipelineName)
        {
            var className = AssetBundleBuilderSetting.GetPackageManifestProcessServicesClassName(packageName, pipelineName);
            var classTypes = EditorTools.GetAssignableTypes(typeof(IManifestProcessServices));
            var classType = classTypes.Find(x => x.FullName.Equals(className));
            if (classType != null)
                return (IManifestProcessServices)Activator.CreateInstance(classType);
            else
                return null;
        }

        /// <summary>
        /// 创建资源清单解密服务类实例
        /// </summary>
        static IManifestRestoreServices CreateManifestRestoreServicesInstance(string packageName, string pipelineName)
        {
            var className = AssetBundleBuilderSetting.GetPackageManifestRestoreServicesClassName(packageName, pipelineName);
            var classTypes = EditorTools.GetAssignableTypes(typeof(IManifestRestoreServices));
            var classType = classTypes.Find(x => x.FullName.Equals(className));
            if (classType != null)
                return (IManifestRestoreServices)Activator.CreateInstance(classType);
            else
                return null;
        }
    }
}