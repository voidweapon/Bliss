using System;
using System.Drawing.Imaging;
using System.Text;

namespace Bliss
{
    public static class BitConverterExtension
    {
        #region Read Adjusted Size

        public static long ToAdaptiveLong(this ReadOnlySpan<byte> buffer)
        {
            int longSize = sizeof(long);
            if (buffer.Length > longSize)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "Buffer length is too large");
            }
        
            if (buffer.Length < longSize)
            {
                return buffer.ToAdaptiveInt();
            }
            
            return BitConverter.ToInt64(buffer);
        }
        
        public static ulong ToAdaptiveULong(this ReadOnlySpan<byte> buffer)
        {
            int longSize = sizeof(ulong);
            if (buffer.Length > longSize)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "Buffer length is too large");
            }
        
            if (buffer.Length < longSize)
            {
                return buffer.ToAdaptiveUInt();
            }
            
            return BitConverter.ToUInt64(buffer);
        }
        
        public static int ToAdaptiveInt(this ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length > sizeof(int))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "Buffer length is too large");
            }
        
            if (buffer.Length == sizeof(short))
            {
                return BitConverter.ToInt16(buffer);
            }
            
            return BitConverter.ToInt32(buffer);
        }
        
        public static uint ToAdaptiveUInt(this ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length > sizeof(uint))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "Buffer length is too large");
            }
        
            if (buffer.Length == sizeof(ushort))
            {
                return BitConverter.ToUInt16(buffer);
            }
            
            return BitConverter.ToUInt32(buffer);
        }

        #endregion
        
        public static bool ToBool(this ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToBoolean(buffer);
        }
        
        public static ushort ToUShort(this ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToUInt16(buffer);
        }
        
        public static short ToShort(this ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToInt16(buffer);
        }
        
        public static uint ToUInt(this ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToUInt32(buffer);
        }
        
        public static int ToInt(this ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToInt32(buffer);
        }

        public static ulong ToULong(this ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToUInt64(buffer);
        }
        
        public static long ToLong(this ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToInt64(buffer);
        }

        public static string ToString(this ReadOnlySpan<byte> buffer)
        {
            return Encoding.ASCII.GetString(buffer);
        }
        
        public static void WriteBool(this Span<byte> buffer, bool value)
        {
            if (buffer.Length < sizeof(bool))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "buffer length is too small");
            }
            BitConverter.TryWriteBytes(buffer, value);
        }
        
        public static void WriteShort(this Span<byte> buffer, short value)
        {
            if (buffer.Length < sizeof(short))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "buffer length is too small");
            }
            BitConverter.TryWriteBytes(buffer, value);
        }
        
        public static void WriteUShort(this Span<byte> buffer, ushort value)
        {
            if (buffer.Length < sizeof(ushort))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "buffer length is too small");
            }
            BitConverter.TryWriteBytes(buffer, value);
        }
        
        public static void WriteInt(this Span<byte> buffer, int value)
        {
            if (buffer.Length < sizeof(int))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "buffer length is too small");
            }
            BitConverter.TryWriteBytes(buffer, value);
        }
        
        public static void WriteUInt(this Span<byte> buffer, uint value)
        {
            if (buffer.Length < sizeof(uint))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "buffer length is too small");
            }
            BitConverter.TryWriteBytes(buffer, value);
        }
        
        public static void WriteLong(this Span<byte> buffer, long value)
        {
            if (buffer.Length < sizeof(long))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "buffer length is too small");
            }
            BitConverter.TryWriteBytes(buffer, value);
        }
        
        public static void WriteULong(this Span<byte> buffer, ulong value)
        {
            if (buffer.Length < sizeof(ulong))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer),  "buffer length is too small");
            }
            BitConverter.TryWriteBytes(buffer, value);
        }
        
        public static int WriteString(this Span<byte> buffer, string value)
        {
            return Encoding.ASCII.GetBytes(value, buffer.ToArray());
        }
        
        
        #region Fluent interface
        
        public static Span<byte> WriteByteTo(this Span<byte> buffer, byte value)
        {
            buffer[0] = value;
            return buffer.Slice(sizeof(byte));
        }
        
        public static Span<byte> WriteBoolTo(this Span<byte> buffer, bool value)
        {
            buffer.WriteBool(value);
            return buffer.Slice(sizeof(bool));
        }

        
        public static Span<byte> WriteShortTo(this Span<byte> buffer, short value)
        {
            buffer.WriteShort(value);
            return buffer.Slice(sizeof(short));
        }
        
        public static Span<byte> WriteUShortTo(this Span<byte> buffer, ushort value)
        {
            buffer.WriteInt(value);
            return buffer.Slice(sizeof(ushort));
        }
        
        public static Span<byte> WriteIntTo(this Span<byte> buffer, int value)
        {
            buffer.WriteInt(value);
            return buffer.Slice(sizeof(int));
        }
        
        public static Span<byte> WriteUIntTo(this Span<byte> buffer, uint value)
        {
            buffer.WriteUInt(value);
            return buffer.Slice(sizeof(uint));
        }

                
        public static Span<byte> WriteLongTo(this Span<byte> buffer, long value)
        {
            buffer.WriteLong(value);
            return buffer.Slice(sizeof(long));
        }
        
        public static Span<byte> WriteULongTo(this Span<byte> buffer, ulong value)
        {
            buffer.WriteULong(value);
            return buffer.Slice(sizeof(ulong));
        }
        
        public static Span<byte> WriteSpanTo(this Span<byte> buffer, Span<byte> sourceBuffer)
        {
            sourceBuffer.CopyTo(buffer);
            return buffer.Slice(sourceBuffer.Length);
        }
        
        public static Span<byte> WriteReadOnlySpanTo(this Span<byte> buffer, ReadOnlySpan<byte> sourceBuffer)
        {
            sourceBuffer.CopyTo(buffer);
            return buffer.Slice(sourceBuffer.Length);
        }
        
        public static Span<byte> WriteStringTo(this Span<byte> buffer, string value)
        {
            int length = Encoding.ASCII.GetBytes(value, buffer.ToArray());
            return buffer.Slice(length);
        }

        public static ReadOnlySpan<byte> ReadByte(this ReadOnlySpan<byte> buffer, out byte value)
        {
            value = buffer[0];
            return buffer.Slice(sizeof(byte));
        }
        
        public static ReadOnlySpan<byte> ReadBool(this ReadOnlySpan<byte> buffer, out bool value)
        {
            value = BitConverter.ToBoolean(buffer);
            return buffer.Slice(sizeof(bool));
        }

        public static ReadOnlySpan<byte> ReadShort(this ReadOnlySpan<byte> buffer, out short value)
        {
            value = BitConverter.ToInt16(buffer);
            return buffer.Slice(sizeof(short));
        }

        public static ReadOnlySpan<byte> ReadUShort(this ReadOnlySpan<byte> buffer, out ushort value)
        {
            value = BitConverter.ToUInt16(buffer);
            return buffer.Slice(sizeof(ushort));
        }

        public static ReadOnlySpan<byte> ReadInt(this ReadOnlySpan<byte> buffer, out int value)
        {
            value = BitConverter.ToInt32(buffer);
            return buffer.Slice(sizeof(int));
        }

        public static ReadOnlySpan<byte> ReadUInt(this ReadOnlySpan<byte> buffer, out uint value)
        {
            value = BitConverter.ToUInt32(buffer);
            return buffer.Slice(sizeof(uint));
        }

        public static ReadOnlySpan<byte> ReadLong(this ReadOnlySpan<byte> buffer, out long value)
        {
            value = BitConverter.ToInt64(buffer);
            return buffer.Slice(sizeof(long));
        }

        public static ReadOnlySpan<byte> ReadULong(this ReadOnlySpan<byte> buffer, out ulong value)
        {
            value = BitConverter.ToUInt64(buffer);
            return buffer.Slice(sizeof(ulong));
        }

        #endregion
    }
}