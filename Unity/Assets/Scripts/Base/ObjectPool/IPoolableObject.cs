using System;
namespace Bliss.ObjectPool
{
    /// <summary>
    /// 可以由对象池创建的对象
    /// </summary>
    public interface IPoolableObject
    {
        /// <summary>
        /// 对象是否由对象池创建
        /// </summary>
        bool IsFromPool { get; internal set; }
        
        /// <summary>
        /// 对象是否已经在对象池中
        /// </summary>
        bool IsInPool { get; internal set; }
    }
}