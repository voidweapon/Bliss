using System;

namespace Bliss
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class MemberVisibleToAttribute : Attribute
    {
        public Type TargetType { get; internal set; }
        public EAccess Access { get; internal set; }
        
        [Flags]
        public enum EAccess
        {
            None = 0,
            Get = 1 << 0,
            Set = 1 << 1,
            All = Get | Set,
        }
        
        public MemberVisibleToAttribute(Type targetType,  EAccess access)
        {
            TargetType = targetType;
            Access = access;
        }
        
    }
}