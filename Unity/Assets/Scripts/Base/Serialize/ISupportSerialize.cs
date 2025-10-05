using System.ComponentModel;

namespace Bliss
{
    public interface ISupportSerialize : ISupportInitialize
    {
        /// <summary>
        /// 序列化之前调用
        /// </summary>
        void BeforeSerialize();
        
        /// <summary>
        /// 序列化之后调用
        /// </summary>
        void AfterSerialize();
    }
}