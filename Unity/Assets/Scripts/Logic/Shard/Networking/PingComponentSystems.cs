using System;
using Bliss.Core;
using ET;

namespace Bliss
{
    [EntitySystemOf(typeof(PingComponent))]
    public static partial class PingComponentSystems
    {
        public static void Awake(this PingComponent self)
        {
            self.OutLagPacketId = new int[PingComponent.OutLagPacketCountLimit];
            self.OutLagPacketSendTime = new long[PingComponent.OutLagPacketCountLimit];
            self.LastPingTimeMS = 0;
            self.RTT_EMA = new ExponentialMovingAverage(PingComponent.RTT_MA_Window, ExponentialMovingAverage.EMode.StandardDeviation);
            self.Jitter_EMA = new ExponentialMovingAverage(4);
        }
        public static void Update(this PingComponent self)
        {
            long nowTIme = TimeInfo.Instance.ClientNow();
            if (nowTIme - self.LastPingTimeMS > self.PingIntervalMS)
            {
                self.LastPingTimeMS = nowTIme;
                self.SendPing();
            }
        }
        public static void Destroy(this PingComponent self)
        {

        }

        private static void SendPing(this PingComponent self)
        {
            long sendTimeMS = TimeInfo.Instance.ClientNow();
            int packetId = self.OutPacketId++;
            
            //记录发送端Ping Msg的PacketId和发送时间
            int outLagPacketIndex = packetId & (PingComponent.OutLagPacketCountLimit - 1);
            self.OutLagPacketId[outLagPacketIndex] = packetId;
            self.OutLagPacketSendTime[outLagPacketIndex] = sendTimeMS;
            
            using var pingMsg =  PingMessage.Create(true);
            pingMsg.PacketId =  packetId;
            pingMsg.LocalTime = sendTimeMS;
            
            var session = self.Parent as Session;
            session.Send(pingMsg, false);
        }

        public static void OnPong(this PingComponent self, PongMessage pongMessage)
        {
            int packetId = pongMessage.PacketId;
            
            int outLagPacketIndex = packetId & (PingComponent.OutLagPacketCountLimit - 1);
            if (self.OutLagPacketId[outLagPacketIndex] != packetId)
            {
                return;
            }
            
            long currentTime = TimeInfo.Instance.ClientNow();
            long sendTimeMS = self.OutLagPacketSendTime[outLagPacketIndex];
            long rttMS = currentTime - sendTimeMS;
            long serverSendTime = pongMessage.LocalTime;

            self.OutLagPacketId[outLagPacketIndex] = -1;
            self.OutLagPacketSendTime[outLagPacketIndex] = 0;

            double jitterMS = 0;
            if (self.RTT_EMA.Value == 0)
            {
                jitterMS = rttMS / 2.0;
            }
            else
            {
                jitterMS = rttMS - self.RTT_EMA.Value;
            }
            self.Jitter_EMA.Add(Math.Abs(jitterMS));
            self.RTT_EMA.Add(rttMS);
            self.RTT = rttMS;

            if (self.IsUpdateServerTime)
            {
                TimeInfo.Instance.ServerMinusClientTime = serverSendTime + (long)(self.RTT_EMA.Value / 2) - currentTime;
            }
        }

        /// <summary>
        /// 收Ping直接回复Pong
        /// </summary>
        /// <param name="self"></param>
        /// <param name="pongMessage"></param>
        public static void OnPing(this PingComponent self, PingMessage pongMessage)
        {
            using var pingMsg =  PongMessage.Create(true);
            pingMsg.PacketId =  pongMessage.PacketId;
            pingMsg.LocalTime = TimeInfo.Instance.ClientNow();
            
            var session = self.Parent as Session;
            session.Send(pingMsg, false);
        }
    }

    [SessionMessageHandler(ELogicSceneType.Main)]
    public sealed class PingMessageHandler : SessionMessageHandler<PingMessage>
    {
        protected override async ETTask Run(Session session, PingMessage message)
        {
            session.GetComponent<PingComponent>().OnPing(message);
            await ETTask.CompletedTask;
        }
    }
    
    [SessionMessageHandler(ELogicSceneType.Main)]
    public sealed class PongMessageHandler : SessionMessageHandler<PongMessage>
    {
        protected override async ETTask Run(Session session, PongMessage message)
        {
            session.GetComponent<PingComponent>().OnPong(message);
            await ETTask.CompletedTask;
        }
    }
}