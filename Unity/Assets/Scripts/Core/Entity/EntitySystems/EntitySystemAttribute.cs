using System;
using Bliss.Core;

namespace Bliss
{
    /// <summary>
    /// Mark Class Work with <see cref="EntitySystemManager"/>, <see cref="EntitySystem"/>
    /// auto generate single object
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EntitySystemAttribute : BaseAttribute
    {
        
    }
}