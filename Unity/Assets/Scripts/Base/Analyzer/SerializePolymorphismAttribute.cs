using System;

namespace Bliss
{
    /// <summary>
    /// 提示SourceGenerator生成多态序列化相关代码
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class SerializePolymorphismAttribute : Attribute
    {
        
    }
    
    
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class SerializePolymorphismRegisterAttribute : Attribute
    {
        
    }
}