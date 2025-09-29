using System;
using System.Buffers;
using System.IO;

namespace Bliss.Core
{
    public class SocketBuffer : MemoryStream, IBufferWriter<byte>
    {
        public int HeadPosition  { get; private set; }
        public int DataPosition { get; private set; }
        public int TailPosition { get; private set; }
        
        public long EndPosition => Length;
        
        public int TrueLength => TailPosition - DataPosition;
        
        
        public ReadOnlyMemory<byte> WrittenMemory => GetBuffer().AsMemory(HeadPosition, (int)Position);

        public ReadOnlySpan<byte> WrittenSpan => GetBuffer().AsSpan(HeadPosition, (int)Position);

        
        public SocketBuffer()
        {
            
        }
        
        public SocketBuffer(int capacity) : base(capacity)
        {
            
        }
        
        public SocketBuffer(byte[] buffer) : base(buffer, 0, buffer.Length, true, true)
        {

        }

        public SocketBuffer(byte[] buffer, int index, int count) : base(buffer, index, count, true, true)
        {
            HeadPosition = DataPosition = TailPosition = index;
        }

        public void Reserve(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative");
            }

            if (DataPosition + count < 0
                || TailPosition + count < 0)
            {
                throw new IOException("too many bytes");
            }
            
            DataPosition += count;
            TailPosition += count;
        }

        public void Put(byte[] buffer, int offset, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative");
            }

            if (TailPosition + count < 0)
            {
                throw new IOException("too many bytes");
            }
            
            Seek(TailPosition, SeekOrigin.Begin);
            Write(buffer, offset, count);
            TailPosition += count;
        }
        
        public Span<byte> Put(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative");
            }

            if (TailPosition + count < 0 || TailPosition + count > Capacity)
            {
                throw new IOException("too many bytes");
            }
            
            Span<byte> span = GetBuffer().AsSpan(TailPosition, count);
            TailPosition += count;
            Seek(TailPosition, SeekOrigin.Begin);
            return span;
        }
        
        public void Push(byte[] buffer, int offset, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative");
            }

            if (DataPosition - count < HeadPosition)
            {
                throw new IOException("too many bytes");
            }
            
            DataPosition -= count;
            Seek(DataPosition, SeekOrigin.Begin);
            Write(buffer, offset, count);
        }
        
        public Span<byte> Push(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative");
            }

            if (DataPosition - count < HeadPosition)
            {
                throw new IOException("too many bytes");
            }
            
            DataPosition -= count;
            Seek(DataPosition, SeekOrigin.Begin);
            return GetSpan(count);
        }

        public ReadOnlySpan<byte> Pull(int count)
        {
            if (DataPosition + count > TailPosition)
            {
                throw new IOException("too many bytes");
            }

            Span<byte> span = GetBuffer().AsSpan(DataPosition, count);
            DataPosition += count;
            Seek(DataPosition, SeekOrigin.Begin);
            
            return span;
        }

        public void Clear()
        {
            Seek(0, SeekOrigin.Begin);
            DataPosition = 0;
            TailPosition = 0;
        }

        
        public void Advance(int count)
        {
            long newLength = Position + count;
            if (newLength > Length)
            {
                SetLength(newLength);
            }
            Position = newLength;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (Length - Position < sizeHint)
            {
                SetLength(Position + sizeHint);
            }
            var memory = GetBuffer().AsMemory((int)Position, (int)(Length - Position));
            return memory;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (Length - Position < sizeHint)
            {
                SetLength(Position + sizeHint);
            }
            var span = GetBuffer().AsSpan((int)Position, (int)(Length - Position));
            return span;
        }
    }
}