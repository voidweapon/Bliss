using System;
using System.ComponentModel;

namespace Bliss
{
    public abstract class DisposableObject : BObject, IDisposable, ISupportSerialize
    {
        public virtual void Dispose()
        {
            
        }

        /// <summary>
        /// 在序列化之前调用
        /// 可以在里面做序列化的准备工作
        /// </summary>
        public virtual void BeforeSerialize()
        {
            
        }

        /// <summary>
        /// 在序列化之后调用
        /// 可以在里面做序列化的清理工作
        /// </summary>
        public virtual void AfterSerialize()
        {
            
        }
        

        /// <summary>
        /// BeforeDeserialize
        /// 在反序列化之前调用
        /// 可以在里面做一些对象初始化工作
        /// 不保证调用
        /// </summary>
        public virtual void BeginInit()
        {
            
        }

        /// <summary>
        /// AfterDeserialize
        /// 在反序列化之后调用
        /// 可以在里面做一些对象初始化工作
        /// 保证调用
        /// </summary>
        public virtual void EndInit()
        {
            
        }
    }
}