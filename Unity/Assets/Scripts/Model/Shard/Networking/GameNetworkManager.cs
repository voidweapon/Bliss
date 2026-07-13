namespace Bliss
{
    public class GameNetworkManager : Singleton<GameNetworkManager>, ISingletonInit
    {
        public float BadPacketLossThreshold;
        public float SeverePacketLossThreshold;
        public int BadPingThreshold;
        public int SeverePingThreshold;

        public float MoveRepSize;
        
        
        public float MaxMoveDeltaTime;
        public float MaxClientSmoothingDeltaTime;
        public float ClientNetSendMoveDeltaTime;
        public float ClientNetSendMoveDeltaTimeThrottled;
        public float ClientNetSendMoveDeltaTimeStationary;
        public int ClientNetSendMoveThrottleAtNetSpeed;
        public int ClientNetSendMoveThrottleOverPlayerCount;
        
        public void Init()
        {
            BadPacketLossThreshold = 0.05f;
            SeverePacketLossThreshold = 0.15f;
            BadPingThreshold = 200;
            SeverePingThreshold = 500;

            MoveRepSize = 42f;
            
            MaxMoveDeltaTime = 0.125f;
            MaxClientSmoothingDeltaTime = 0.50f;
            ClientNetSendMoveDeltaTime = 0.0166f;
            ClientNetSendMoveDeltaTimeThrottled = 0.0222f;
            ClientNetSendMoveDeltaTimeStationary = 0.0166f;
            ClientNetSendMoveThrottleAtNetSpeed = 10000;
            ClientNetSendMoveThrottleOverPlayerCount = 10;
        }
    }
}