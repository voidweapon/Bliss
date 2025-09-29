using System;
using Bliss.Core.Network;

namespace Bliss.Core
{
    [WeakRefConstraint]
    public struct SocketBufferRef
    {
        public int PoolIndex { get; private set; }
        private WeakReference<SocketBuffer> _socketBufferRef;
        private WeakReference<ANetDriver> _driverRef;

        public SocketBuffer Target
        {
            get
            {
                if (_socketBufferRef == null)
                {
                    return null;
                }

                if (_socketBufferRef.TryGetTarget(out var socketBuffer))
                {
                    return socketBuffer;
                }
                
                return null;
            }
        }
        public SocketBufferRef(int poolIndex, SocketBuffer socketBuffer, ANetDriver driver)
        {
            PoolIndex = poolIndex;
            _socketBufferRef = new WeakReference<SocketBuffer>(socketBuffer);
            _driverRef =  new WeakReference<ANetDriver>(driver);
        }
        
        public void Dispose()
        {
            if (_driverRef != null
                && _driverRef.TryGetTarget(out var driver)
                && _socketBufferRef != null
                && _socketBufferRef.TryGetTarget(out var socketBuffer))
            {
                driver.ReturnBuffer(PoolIndex, socketBuffer);
                _driverRef = null;
                _socketBufferRef = null;
            }
        }
    }
}