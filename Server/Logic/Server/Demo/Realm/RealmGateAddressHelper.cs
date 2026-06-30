using System.Collections.Generic;

namespace Bliss;

public  static class RealmGateAddressHelper
{
    public static ServerScene GetGate(int zone, string account)
    {
        ulong hash = (ulong)account.GetLongHashCode();
			
        List<ServerScene> zoneGates = ConfigManager.Instance.Tables.TbServerScene.DicZone2Gates[zone];
			
        return zoneGates[(int)(hash % (ulong)zoneGates.Count)];
    }
}