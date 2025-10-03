using System;

namespace Bliss
{
    /// <summary>
    /// 提示分析器禁止通过new来构造对象
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct, Inherited = true)]
    public class DisableNewAttribute : Attribute
    {
    
    }
}

