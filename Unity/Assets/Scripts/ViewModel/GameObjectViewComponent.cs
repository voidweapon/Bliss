using System.Collections.Generic;
using UnityEngine;
using Drawing;
using ET;

namespace Bliss.ViewModel
{
    /// <summary>
    /// Unity侧的View组件
    /// 经常为空
    /// </summary>
    public class GameObjectViewComponent : Entity, IAwake<GameObjectViewComponentInitParam>, IDestroy, ILateUpdate
    {

        /// <summary>
        /// 数据节点,同步创建
        /// </summary>
        public GameObject View;

        /// <summary>
        /// 视图节点,经常为空,异步加载
        /// </summary>
        public GameObject ViewMode;

        /// <summary>
        /// 视图节点路径
        /// </summary>
        public string ViewAssetPath;
        public string ViewModelPhysicalAssetPath;
        public string ViewModelViewAssetPath;

        /// <summary>
        /// 视图节点加载Task的CancellationToken
        /// </summary>
        public ETCancellationToken ViewModelLoadCancellationToken;
        
        /// <summary>
        /// 视图节点如何如何创建的,决定如何销毁
        /// </summary>
        public bool IsViewModelFromPool;
        
        /// <summary>
        /// 视图节点子节点路径换成
        /// </summary>
        public Dictionary<string, Transform> DicPathTpTransform = new Dictionary<string, Transform>();
        
        public TransformComponent  TransformComponent;
        
#if UNITY_EDITOR
        public Camera[] DrawTargets;
        public Camera[] DrawTargets2;
#endif
    }

    public struct GameObjectViewComponentInitParam
    {
        public GameObject View;
        public string ViewAssetPath;
        public bool IsFromPool;
    }
}