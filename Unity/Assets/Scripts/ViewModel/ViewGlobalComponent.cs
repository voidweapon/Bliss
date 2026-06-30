using Unity.Cinemachine;
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
        /// 全局实体挂载节点
        /// </summary>
        public Transform EntitiesTransform;

        /// <summary>
        /// UGUI 挂载节点
        /// </summary>
        public Transform UGUI_Canvas;
        
        public Transform UGUI_Canvas_Low;
        public Transform UGUI_Canvas_Mid;
        public Transform UGUI_Canvas_High;
        public Transform UGUI_Canvas_Debug;
        
        
        public CinemachineVirtualCamera PlayerVirtualCamera;
    }
}