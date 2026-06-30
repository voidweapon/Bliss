using UnityEngine;
using YooAsset;

namespace Bliss
{
    [CreateAssetMenu(fileName = "BootSetting", menuName = "Bliss/Boost/BootSetting")]
    public class BootSetting : ScriptableObject
    {
        /// <summary>
        /// 视图层目标帧率
        /// </summary>
        public int TargetFPS = 60;
        
        /// <summary>
        /// YooAsset 模式
        /// </summary>
        public EPlayMode PlayMode = EPlayMode.OfflinePlayMode;

        public string AppVersion = "1.0.0";
        public string ResDefaultHostServer = "http://127.0.0.1";
        public string ResFallbackHostServer = "http://127.0.0.1";
        
        #region 物理设置
        public bool AutoSimulatePhysics = false;
        public bool AutoSyncPhysicalTransform = false;
        #endregion

        #region 编辑器下测试用
        public bool TestABInEditor = false;
        public bool TestDLLInEditor = false;
        #endregion

        
    }
}