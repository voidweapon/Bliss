using UnityEngine;

namespace Bliss.ViewModel
{
    public class ViewGlobalComponent : Entity, IAwake
    {
        /// <summary>
        /// 全局对象挂载节点
        /// </summary>
        public Transform GlobalTransform;

        /// <summary>
        /// UGUI 挂载节点
        /// </summary>
        public Transform UGUI_Canvas;
        
        public Transform UGUI_Canvas_Low;
        public Transform UGUI_Canvas_Mid;
        public Transform UGUI_Canvas_High;
        public Transform UGUI_Canvas_Debug;
    }
}