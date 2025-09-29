using System;

namespace Bliss
{
    public  static class ByteAryHelper
    {
        public static void WriteBool(this byte[] buffer, int offset, bool value)
        {
            buffer.AsSpan(offset).WriteBool(value);
        }
        
        public static void WriteShort(this byte[] buffer, int offset, short value)
        {
            buffer.AsSpan(offset).WriteShort(value);
        }
        
        public static void WriteUShort(this byte[] buffer, int offset, ushort value)
        {
            buffer.AsSpan(offset).WriteUShort(value);
        }
  
        public static void WriteInt(this byte[] buffer, int offset, int value)
        {
            buffer.AsSpan(offset).WriteInt(value);
        }

        
        public static void WriteUInt(this byte[] buffer, int offset, uint value)
        {
            buffer.AsSpan(offset).WriteUInt(value);
        }

        
        public static void WriteLong(this byte[] buffer, int offset, long value)
        {
            buffer.AsSpan(offset).WriteLong(value);
        }
        
        
        public static void WriteULong(this byte[] buffer, int offset, ulong value)
        {
            buffer.AsSpan(offset).WriteULong(value);
        }
    }
}