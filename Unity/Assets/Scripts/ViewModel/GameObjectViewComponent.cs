using UnityEngine;
using Drawing;

namespace Bliss.ViewModel
{
    /// <summary>
    /// Unity侧的View组件
    /// 经常为空
    /// </summary>
    public class GameObjectViewComponent : Entity, IAwake<GameObjectViewComponentInitParam>, IDestroy, IUpdate
    {
        /// <summary>
        /// 经常为空
        /// </summary>
        public GameObject View;
        public bool IsViewFromPool;
        
        public Camera[] DrawTargets;
        public Camera[] DrawTargets2;
    }

    public struct GameObjectViewComponentInitParam
    {
        public GameObject View;
        public bool IsFromPool;
    }
}