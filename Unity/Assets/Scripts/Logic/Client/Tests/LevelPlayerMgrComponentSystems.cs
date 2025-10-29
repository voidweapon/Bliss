using Bliss.Core;

namespace Bliss
{
    public  static class LevelPlayerMgrComponentSystems
    {
        public static void Broadcast(this LevelPlayerMgrComponent self, MessageObject message, bool isReliable = true)
        {
            if(self.ChildCount == 0) return;

            SocketBufferRef? messageStream = null;

            foreach (var kv in self.Children)
            {
                var player = kv.Value as Player;
                var session = player?.GetComponent<PlayerNetworkComponent>()?.Session;
                if(session == null) continue;

                if (messageStream == null)
                {
                    messageStream = session.MessageToNetStream(message);
                }
                
                session.SendRawCopy(messageStream.Value, isReliable);
            }
            
            messageStream?.Dispose();
        }
    }
}