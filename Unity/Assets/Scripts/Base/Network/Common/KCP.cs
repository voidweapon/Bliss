using System;
using kcp;

namespace Bliss.Core
{
    /// <summary>
    /// 包装kcp对象
    /// </summary>
    public unsafe class KCP : IDisposable
    {
        private IKCPCB* _kcp;
        private KCP_OutPutCallback _outPutCallback;

        public KCP(uint conv, KCP_OutPutCallback  outPutCallback)
        {
            _kcp = KCP_INTERFACE.ikcp_create(conv, null);
            _outPutCallback = outPutCallback;
        }
        
        public void Dispose()
        {
            KCP_INTERFACE.ikcp_release(_kcp);
        }
        
        public int ikcp_input(ReadOnlySpan<byte> buffer)
        {
            fixed (byte* bufferPtr = &buffer[0])
            {
                return KCP_INTERFACE.ikcp_input(_kcp, bufferPtr, buffer.Length);
            }
        }
        
        public int ikcp_recv(Span<byte> buffer)
        {
            fixed (byte* bufferPtr = &buffer[0])
            {
                return KCP_INTERFACE.ikcp_recv(_kcp, bufferPtr, buffer.Length);
            }
        }

        public int ikcp_send(ReadOnlySpan<byte> buffer)
        {
            fixed (byte* bufferPtr = &buffer[0])
            {
                return KCP_INTERFACE.ikcp_send(_kcp, bufferPtr, buffer.Length);
            }
        }

        public void ikcp_update(uint current)
        {
            kcp.KCP.ikcp_update(_kcp, current, _outPutCallback);
        }
        
        public void ikcp_flush()
        {
            kcp.KCP.ikcp_flush(_kcp, _outPutCallback);
        }

        public uint ikcp_check(uint current)
        {
            return KCP_INTERFACE.ikcp_check(_kcp, current);
        }

        public int ikcp_waitsnd()
        {
            return KCP_INTERFACE.ikcp_waitsnd(_kcp);
        }

        public int ikcp_peeksize()
        {
            return KCP_INTERFACE.ikcp_peeksize(_kcp);
        }
        
        public int ikcp_nodelay(int nodelay, int interval, int resend, int nc)
        {
            return KCP_INTERFACE.ikcp_nodelay(_kcp, nodelay, interval, resend, nc);
        }
        
        public int ikcp_wndsize(int sndwnd, int rcvwnd)
        {
            return KCP_INTERFACE.ikcp_wndsize(_kcp, sndwnd, rcvwnd);
        }

        public int ikcp_setmtu(int mtu)
        {
            return KCP_INTERFACE.ikcp_setmtu(_kcp, mtu);
        }

        public int ikcp_state()
        {
            return (int)_kcp->state;
        }
    }
}