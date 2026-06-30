using System;
using System.Runtime.InteropServices;
using MemoryPack;

namespace Bliss.Gameplay.GameplayAttribute
{
    [MemoryPackable]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public partial struct PrimitiveValue : IEquatable<PrimitiveValue>
    {
        [FieldOffset(0)]
        public long RawVar;

        #region Operators
        public static implicit operator long(PrimitiveValue property)
        {
            return property.RawVar;
        }
        
        public static implicit operator int(PrimitiveValue property)
        {
            return (int)property.RawVar;
        }
        
        public static implicit operator uint(PrimitiveValue property)
        {
            return (uint)property.RawVar;
        }
        
        public static implicit operator short(PrimitiveValue property)
        {
            return (short)property.RawVar;
        }
        
        public static implicit operator ushort(PrimitiveValue property)
        {
            return (ushort)property.RawVar;
        }
        
        public static implicit operator bool(PrimitiveValue var)
        {
            return var.RawVar > 0;
        }
        
        public static implicit operator PrimitiveValue(long property)
        {
            return new PrimitiveValue { RawVar = property };
        }
        
        public static implicit operator PrimitiveValue(int property)
        {
            return new PrimitiveValue { RawVar = property };
        }
        
        public static implicit operator PrimitiveValue(uint property)
        {
            return new PrimitiveValue { RawVar = property };
        }
        
        public static implicit operator PrimitiveValue(short property)
        {
            return new PrimitiveValue { RawVar = property };
        }
        
        public static implicit operator PrimitiveValue(ushort property)
        {
            return new PrimitiveValue { RawVar = property };
        }
        
        public static implicit operator PrimitiveValue(bool var)
        {
            return new PrimitiveValue { RawVar = var ? 1L : 0L };
        }
        #endregion

        
        public static bool operator ==(PrimitiveValue lhs, PrimitiveValue rhs)
        {
            return lhs.RawVar == rhs.RawVar;
        }
        public static bool operator !=(PrimitiveValue lhs, PrimitiveValue rhs)
        {
            return lhs.RawVar != rhs.RawVar;
        }
        public bool Equals(PrimitiveValue other)
        {
            return RawVar == other.RawVar;
        }
        public override bool Equals(object obj)
        {
            return obj is PrimitiveValue other && Equals(other);
        }
        public override int GetHashCode()
        {
            return RawVar.GetHashCode();
        }
    }
}