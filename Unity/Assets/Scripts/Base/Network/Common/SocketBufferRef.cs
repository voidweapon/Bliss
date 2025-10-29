using System;
using Bliss.Core.Network;

namespace Bliss.Core
{
    [WeakRefConstraint]
    public struct SocketBufferRef
    {
        public int PoolIndex { get; private set; }
        private SocketBuffer _socketBufferRef;
        private WeakReference<ANetDriver> _driverRef;

        public SocketBuffer Target
        {
            get
            {
                if (_socketBufferRef == null)
                {
                    return null;
                }
                
                if (_socketBufferRef.Owner.Count != 1)
                {
                    throw new Exception("multi thread error in get");
                }

                if (!_socketBufferRef.Owner.TryGetValue(id, out  _))
                {
                    throw new Exception("multi thread error in get");
                }
                
                return _socketBufferRef;
            }
        }

        private Guid id;
        
        public SocketBufferRef(int poolIndex, SocketBuffer socketBuffer, ANetDriver driver)
        {
            id = Guid.NewGuid();
            PoolIndex = poolIndex;
            _socketBufferRef = socketBuffer;
            _driverRef =  new WeakReference<ANetDriver>(driver);

            if (!socketBuffer.Owner.TryAdd(id, this))
            {
                throw new Exception("multi thread error in add");
            }
        }
        
        public void Dispose()
        {
            if (_driverRef != null
                && _driverRef.TryGetTarget(out var driver)
                && _socketBufferRef != null)
            {
                int poolIndex = PoolIndex;
                var socketBuffer = _socketBufferRef;
                PoolIndex = 0;
                _driverRef = null;
                _socketBufferRef = null;
                
                if (!socketBuffer.Owner.TryRemove(id, out _))
                {
                    throw new Exception("multi thread error in remove");
                }
                
                driver.ReturnBuffer(poolIndex, socketBuffer);
            }
        }
    }
}