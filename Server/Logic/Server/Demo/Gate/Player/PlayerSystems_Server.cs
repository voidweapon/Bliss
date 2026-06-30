namespace Bliss;

public static partial class PlayerSystems_Server
{
    public static void Offline(this Player self)
    {
        long unitId = self.Id;
        Log.Info($"Player {unitId} offline");
        
        G2M_PlayerOfflineMessage g2M_PlayerOfflineMessage = G2M_PlayerOfflineMessage.Create();
        self.RootScene().GetComponent<MessageLocationSenderManager>().Get((int)ENetworkLocationCategory.Unit).Send(unitId, g2M_PlayerOfflineMessage);
        
        var playerMgr = self.RootScene().GetComponent<PlayerManagerComponent>();
        playerMgr.Remove(self);
    }
}