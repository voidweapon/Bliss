using System;

namespace Bliss
{
    /// <summary>
    /// 用于分析器分析，来限制WeakRef的Target传播
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct  | AttributeTargets.Class)]
    public class WeakRefConstraintAttribute : Attribute
    {
        
    }
}