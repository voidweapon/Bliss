#pragma warning disable CA2211
#pragma warning disable CS1591
#pragma warning disable CS8602
#pragma warning disable CS8632

namespace kcp
{
    public unsafe delegate int KCP_OutPutCallback(byte* buffer, int size);
    public static unsafe partial class KCP
    {
     
        /// <summary>
        /// ikcp_output bypass
        /// </summary>
        public static int ikcp_output(IKCPCB* kcp, void* data, int size, KCP_OutPutCallback output)
        {
            assert(kcp != null);
            assert(output != null);
            if (ikcp_canlog(kcp, (int)IKCP_LOG_OUTPUT) != 0)
            {
                ikcp_log(kcp, (int)IKCP_LOG_OUTPUT, "[RO] {0} bytes", (long)size);
            }

            if (size == 0) return 0;
            return output((byte*)data, size);
        }

        /// <summary>
        /// ikcp_flush bypass
        /// </summary>
        public static void ikcp_flush(IKCPCB* kcp, KCP_OutPutCallback output)
        {
            uint current = kcp->current;
            byte* buffer = kcp->buffer;
            byte* ptr = buffer;
            int count, size, i;
            uint resent, cwnd;
            uint rtomin;
            IQUEUEHEAD* p;
            int change = 0;
            int lost = 0;
            IKCPSEG seg;

            // 'ikcp_update' haven't been called. 
            if (kcp->updated == 0) return;

            seg.conv = kcp->conv;
            seg.cmd = IKCP_CMD_ACK;
            seg.frg = 0;
            seg.wnd = (uint)ikcp_wnd_unused(kcp);
            seg.una = kcp->rcv_nxt;
            seg.len = 0;
            seg.sn = 0;
            seg.ts = 0;

            // flush acknowledges
            count = (int)kcp->ackcount;
            for (i = 0; i < count; i++)
            {
                size = (int)(ptr - buffer);
                if (size + (int)IKCP_OVERHEAD > (int)kcp->mtu)
                {
                    ikcp_output(kcp, buffer, size, output);
                    ptr = buffer;
                }

                ikcp_ack_get(kcp, i, &seg.sn, &seg.ts);
                ptr = ikcp_encode_seg(ptr, &seg);
            }

            kcp->ackcount = 0;

            // probe window size (if remote window size equals zero)
            if (kcp->rmt_wnd == 0)
            {
                if (kcp->probe_wait == 0)
                {
                    kcp->probe_wait = IKCP_PROBE_INIT;
                    kcp->ts_probe = kcp->current + kcp->probe_wait;
                }
                else
                {
                    if (_itimediff(kcp->current, kcp->ts_probe) >= 0)
                    {
                        if (kcp->probe_wait < IKCP_PROBE_INIT)
                            kcp->probe_wait = IKCP_PROBE_INIT;
                        kcp->probe_wait += kcp->probe_wait / 2;
                        if (kcp->probe_wait > IKCP_PROBE_LIMIT)
                            kcp->probe_wait = IKCP_PROBE_LIMIT;
                        kcp->ts_probe = kcp->current + kcp->probe_wait;
                        kcp->probe |= IKCP_ASK_SEND;
                    }
                }
            }
            else
            {
                kcp->ts_probe = 0;
                kcp->probe_wait = 0;
            }

            // flush window probing commands
            if ((kcp->probe & IKCP_ASK_SEND) != 0)
            {
                seg.cmd = IKCP_CMD_WASK;
                size = (int)(ptr - buffer);
                if (size + (int)IKCP_OVERHEAD > (int)kcp->mtu)
                {
                    ikcp_output(kcp, buffer, size, output);
                    ptr = buffer;
                }

                ptr = ikcp_encode_seg(ptr, &seg);
            }

            // flush window probing commands
            if ((kcp->probe & IKCP_ASK_TELL) != 0)
            {
                seg.cmd = IKCP_CMD_WINS;
                size = (int)(ptr - buffer);
                if (size + (int)IKCP_OVERHEAD > (int)kcp->mtu)
                {
                    ikcp_output(kcp, buffer, size, output);
                    ptr = buffer;
                }

                ptr = ikcp_encode_seg(ptr, &seg);
            }

            kcp->probe = 0;

            // calculate window size
            cwnd = _imin_(kcp->snd_wnd, kcp->rmt_wnd);
            if (kcp->nocwnd == 0) cwnd = _imin_(kcp->cwnd, cwnd);

            // move data from snd_queue to snd_buf
            while (_itimediff(kcp->snd_nxt, kcp->snd_una + cwnd) < 0)
            {
                IKCPSEG* newseg;
                if (iqueue_is_empty(&kcp->snd_queue)) break;

                newseg = iqueue_entry<IKCPSEG>(kcp->snd_queue.next);

                iqueue_del(&newseg->node);
                iqueue_add_tail(&newseg->node, &kcp->snd_buf);
                kcp->nsnd_que--;
                kcp->nsnd_buf++;

                newseg->conv = kcp->conv;
                newseg->cmd = IKCP_CMD_PUSH;
                newseg->wnd = seg.wnd;
                newseg->ts = current;
                newseg->sn = kcp->snd_nxt++;
                newseg->una = kcp->rcv_nxt;
                newseg->resendts = current;
                newseg->rto = (uint)kcp->rx_rto;
                newseg->fastack = 0;
                newseg->xmit = 0;
            }

            // calculate resent
            resent = (kcp->fastresend > 0) ? (uint)kcp->fastresend : 0xffffffff;
            rtomin = (uint)((kcp->nodelay == 0) ? (kcp->rx_rto >> 3) : 0);

            // flush data segments
            for (p = kcp->snd_buf.next; p != &kcp->snd_buf; p = p->next)
            {
                IKCPSEG* segment = iqueue_entry<IKCPSEG>(p);
                int needsend = 0;
                if (segment->xmit == 0)
                {
                    needsend = 1;
                    segment->xmit++;
                    segment->rto = (uint)kcp->rx_rto;
                    segment->resendts = current + segment->rto + rtomin;
                }
                else if (_itimediff(current, segment->resendts) >= 0)
                {
                    needsend = 1;
                    segment->xmit++;
                    kcp->xmit++;
                    if (kcp->nodelay == 0)
                    {
                        segment->rto += _imax_(segment->rto, (uint)kcp->rx_rto);
                    }
                    else
                    {
                        int step = (kcp->nodelay < 2) ? ((int)(segment->rto)) : kcp->rx_rto;
                        segment->rto += (uint)(step / 2);
                    }

                    segment->resendts = current + segment->rto;
                    lost = 1;
                }
                else if (segment->fastack >= resent)
                {
                    if ((int)segment->xmit <= kcp->fastlimit ||
                        kcp->fastlimit <= 0)
                    {
                        needsend = 1;
                        segment->xmit++;
                        segment->fastack = 0;
                        segment->resendts = current + segment->rto;
                        change++;
                    }
                }

                if (needsend != 0)
                {
                    int need;
                    segment->ts = current;
                    segment->wnd = seg.wnd;
                    segment->una = kcp->rcv_nxt;

                    size = (int)(ptr - buffer);
                    need = (int)(IKCP_OVERHEAD + segment->len);

                    if (size + need > (int)kcp->mtu)
                    {
                        ikcp_output(kcp, buffer, size, output);
                        ptr = buffer;
                    }

                    ptr = ikcp_encode_seg(ptr, segment);

                    if (segment->len > 0)
                    {
                        memcpy(ptr, segment->data, segment->len);
                        ptr += segment->len;
                    }

                    if (segment->xmit >= kcp->dead_link)
                    {
                        kcp->state = unchecked((uint)(-1));
                    }
                }
            }

            // flash remain segments
            size = (int)(ptr - buffer);
            if (size > 0)
            {
                ikcp_output(kcp, buffer, size, output);
            }

            // update ssthresh
            if (change != 0)
            {
                uint inflight = kcp->snd_nxt - kcp->snd_una;
                kcp->ssthresh = inflight / 2;
                if (kcp->ssthresh < IKCP_THRESH_MIN)
                    kcp->ssthresh = IKCP_THRESH_MIN;
                kcp->cwnd = kcp->ssthresh + resent;
                kcp->incr = kcp->cwnd * kcp->mss;
            }

            if (lost != 0)
            {
                kcp->ssthresh = cwnd / 2;
                if (kcp->ssthresh < IKCP_THRESH_MIN)
                    kcp->ssthresh = IKCP_THRESH_MIN;
                kcp->cwnd = 1;
                kcp->incr = kcp->mss;
            }

            if (kcp->cwnd < 1)
            {
                kcp->cwnd = 1;
                kcp->incr = kcp->mss;
            }
        }
        
        /// <summary>
        /// ikcp_update bypass
        /// </summary>
        public static void ikcp_update(IKCPCB* kcp, uint current, KCP_OutPutCallback output)
        {
            int slap;

            kcp->current = current;

            if (kcp->updated == 0)
            {
                kcp->updated = 1;
                kcp->ts_flush = kcp->current;
            }

            slap = (int)_itimediff(kcp->current, kcp->ts_flush);

            if (slap >= 10000 || slap < -10000)
            {
                kcp->ts_flush = kcp->current;
                slap = 0;
            }

            if (slap >= 0)
            {
                kcp->ts_flush += kcp->interval;
                if (_itimediff(kcp->current, kcp->ts_flush) >= 0)
                {
                    kcp->ts_flush = kcp->current + kcp->interval;
                }

                ikcp_flush(kcp, output);
            }
        }
    }
}