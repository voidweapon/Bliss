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
        /// 在反序列化之后调用
        /// 可以在里面做一些对象初始化工作
        /// </summary>
        public virtual void AfterDeserialize()
        {
            
        }
    }
}