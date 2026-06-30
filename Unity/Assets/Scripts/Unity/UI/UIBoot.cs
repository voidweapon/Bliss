using System;
using Bliss;
using Bliss.SimpleUI;
using Bliss.Timer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace Unity.UI
{
    public class UIBoot : ASimpleUI
    {
        private Slider Slider;
        public TextMeshProUGUI Text_Stage;
        public TextMeshProUGUI Text_Percent;
        public TextMeshProUGUI Text_Info;
        
        private WeakReference<DownloaderOperation> downloaderOperationRef;
        private WeakReference<ClearCacheFilesOperation> clearCacheFilesOperationRef;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            var referenceCollector = UIObject.GetComponent<ReferenceCollector>();
            Slider = referenceCollector.Get<GameObject>("Slider").GetComponent<Slider>();
            Text_Stage = referenceCollector.Get<GameObject>("Text_Stage").GetComponent<TextMeshProUGUI>();
            Text_Percent = referenceCollector.Get<GameObject>("Text_Percent").GetComponent<TextMeshProUGUI>();
            Text_Info = referenceCollector.Get<GameObject>("Text_Info").GetComponent<TextMeshProUGUI>();

            Slider.value = 0;
            Text_Stage.text = string.Empty;
            Text_Percent.text = string.Empty;
            Text_Info.text = string.Empty;
            
            SimpleResourcesComponent.Instance.OnDownloadEvent += OnDownloadEvent;
            SimpleResourcesComponent.Instance.OnClearCacheFileEvent += OnClearCacheFileEvent;
        }

        override protected void OnDestroy()
        {
            SimpleResourcesComponent.Instance.OnDownloadEvent -= OnDownloadEvent;
            SimpleResourcesComponent.Instance.OnClearCacheFileEvent -= OnClearCacheFileEvent;
        }

        private void OnDownloadEvent(WeakReference<DownloaderOperation> obj)
        {
            downloaderOperationRef = obj;
            UpdateDownloadStage();
        }
        
        private void OnClearCacheFileEvent(WeakReference<ClearCacheFilesOperation> obj)
        {
            clearCacheFilesOperationRef = obj;
            UpdateClearStage();
        }
        
        private void UpdateDownloadStage()
        {
            if (downloaderOperationRef != null)
            {
                if (downloaderOperationRef.TryGetTarget(out var downloaderOperation))
                {
                    Slider.value = downloaderOperation.Progress;
                    Text_Stage.text = $"Stage: 开始下载资源";
                    Text_Percent.text = $"Progress: {downloaderOperation.Progress * 100:F2}%";
                    Text_Info.text = $"Downloaded: {downloaderOperation.CurrentDownloadBytes / 1024.0f / 1024.0f:F2} MB / Total: {downloaderOperation.TotalDownloadBytes / 1024.0f / 1024.0f:F2} MB";
                }
            }
        }
        
        private void UpdateClearStage()
        {
            if (clearCacheFilesOperationRef != null)
            {
                if (clearCacheFilesOperationRef.TryGetTarget(out var clearCacheFilesOperation))
                {
                    Slider.value = clearCacheFilesOperation.Progress;
                    Text_Stage.text = $"Stage: 清理缓存文件";
                    Text_Percent.text = $"Progress: {clearCacheFilesOperation.Progress * 100:F2}%";
                    Text_Info.text = string.Empty;
                }
            }
        }
        
        
    }
}