using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace Bliss.IO
{
    public class RingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Position { get; set; }

        public override long Length
        {
            get
            {
                if (_buffer.Count == 0)
                {
                    return 0;
                }
                
                return (_buffer.Count - 1) * ChunkSize + (_tailInChunk - _headInChunk);
            }
        }

        public int ChunkSize { get; private set; } = 8192;
        
        private bool _isOpen = false;
        private int _headInChunk;
        private int _tailInChunk;
        private Queue<byte[]> _buffer = new Queue<byte[]>();
        private Queue<byte[]> _pool = new Queue<byte[]>();
        private byte[] _tailChunk;
        
        public RingStream()
        {
            init();
        }
        
        public RingStream(int chunkSize)
        {
            ChunkSize = chunkSize;
            init();
        }

        private void init()
        {
            _headInChunk = 0;
            _tailInChunk = 0;
            _isOpen = true;
            EnqueueChunk();
        }
        
        public override void Flush()
        {
            throw new NotImplementedException();
        }
        
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        private byte[] HeadChunk
        {
            get
            {
                if (_buffer.Count == 0)
                {
                    EnqueueChunk();
                }
                return _buffer.Peek();
            }
        }

        private byte[] TailChunk
        {
            get
            {
                if (_buffer.Count == 0)
                {
                    EnqueueChunk();
                }
                return _tailChunk;
            }
        }
        
        /// <summary>
        /// 从当前流中读取字节块并将数据写入缓冲区。
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer.Length < offset + count)
            {
                throw new ArgumentException($"buff length {buffer.Length} exceeds buffer size {offset + count}");
            }

            count = System.Math.Min((int)Length, count);
            
            int read = 0;
            while (read < count)
            {
                int readCount = count - read;
                if (_headInChunk + readCount < ChunkSize)
                {
                    Array.Copy(HeadChunk, _headInChunk, buffer, offset + read, readCount);
                    read += readCount;
                    _headInChunk += readCount;
                }
                else
                {
                    Array.Copy(HeadChunk, _headInChunk, buffer, offset + read, ChunkSize - _headInChunk);
                    read += ChunkSize - _headInChunk;
                    DequeueChunk();
                    _headInChunk = 0;
                }
            }

            return count;
        }

        /// <summary>
        /// 从当前流中读取字节块并将数据写入目标流
        /// </summary>
        /// <param name="destinationStream"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int Read(Stream destinationStream, int count)
        {
            count = System.Math.Min((int)Length, count);
            
            int read = 0;
            while (read < count)
            {
                int readCount = count - read;
                if (_headInChunk + readCount < ChunkSize)
                {
                    destinationStream.Write(HeadChunk, _headInChunk, readCount);
                    read += readCount;
                    _headInChunk += readCount;
                }
                else
                {
                    destinationStream.Write(HeadChunk, _headInChunk, ChunkSize - _headInChunk);
                    read += ChunkSize - _headInChunk;
                    DequeueChunk();
                    _headInChunk = 0;
                }
            }

            return count;
        }
        
        /// <summary>
        /// 使用从缓冲区读取的数据将字节块写入当前流。
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureCapacity(_tailInChunk + count);
            int written = 0;
            while (written < count)
            {
                if (_tailInChunk == ChunkSize)
                {
                    EnqueueChunk();
                    _tailInChunk = 0;
                }
                
                int writeCount = count - written;
                if (_tailInChunk + writeCount < ChunkSize)
                {
                    Array.Copy(buffer, offset + written, TailChunk, _tailInChunk, writeCount);
                    written += writeCount;
                    _tailInChunk += writeCount;
                }
                else
                {
                    Array.Copy(buffer, offset + written, TailChunk, _tailInChunk, ChunkSize - _tailInChunk);
                    written += (ChunkSize - _tailInChunk);
                    _tailInChunk = ChunkSize;
                }
            }
        }

        
        /// <summary>
        /// 使用从流取的数据将字节块写入当前流。
        /// </summary>
        /// <param name="sourceStream"></param>
        public void Write(Stream sourceStream)
        {
            int count = (int)(sourceStream.Length -  sourceStream.Position);
            EnsureCapacity(_tailInChunk + count);
            
            int written = 0;
            while (written < count)
            {
                if (_tailInChunk == ChunkSize)
                {
                    EnqueueChunk();
                    _tailInChunk = 0;
                }
                
                int writeCount = count - written;
                if (_tailInChunk + writeCount < ChunkSize)
                {
                    sourceStream.Read(TailChunk, _tailInChunk, writeCount);
                    written += writeCount;
                    _tailInChunk += writeCount;
                }
                else
                {
                    sourceStream.Read(TailChunk, _tailInChunk, ChunkSize - _tailInChunk);
                    written += (ChunkSize - _tailInChunk);
                    _tailInChunk = ChunkSize;
                }
            }
        }

        private void EnsureCapacity(int value)
        {
            // Check for overflow
            if (value < 0)
                throw new IOException();
        }

        private void EnqueueChunk()
        {
            byte[] chunk = null;
            if (_pool.Count > 0)
            {
                chunk = _pool.Dequeue();
            }
            else
            {
                chunk = new byte[ChunkSize];
            }
            _buffer.Enqueue(chunk);
            _tailChunk = chunk;
        }

        private void DequeueChunk()
        {
            _pool.Enqueue(_buffer.Dequeue());
        }
        
        private void EnsureNotClosed()
        {
            if(!_isOpen)
                throw new System.ObjectDisposedException(null);
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isOpen = false;
            }
        }
    }
}