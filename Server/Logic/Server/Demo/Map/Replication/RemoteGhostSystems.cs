using Bliss.Core;
using Bliss.Pool;

namespace Bliss;

[EntitySystemOf(typeof(RemoteGhost))]
public static partial class RemoteGhostSystems
{
    private static void Awake(this RemoteGhost self)
    {
        self.Version = TimeInfo.Instance.ServerNow();
        self.ClientVersion = 0;
    }
    private static void Destroy(this RemoteGhost self)
    {
        self.ClientVersion = 0;
        
        foreach (var info in self.RemoteGhostInfos.Values)
        {
            ObjectPool.Instance.Recycle(info);
        }
        self.RemoteGhostInfos.Clear();
    }

    public static void UpdateClientVersion(this RemoteGhost self, long version)
    {
        self.ClientVersion = version;
    }
    
    public static void Add(this RemoteGhost self, long entityId, long version, long timeStamp)
    {
       if(self.RemoteGhostInfos.TryGetValue(entityId, out var info))
       {
           // Update existing info
           self.UpdateVersion(entityId, version, timeStamp);
       }
       else
       {
           // Add new info
           var newInfo = ObjectPool.Instance.Fetch<RemoteGhostInfo>();
           self.RemoteGhostInfos[entityId] = newInfo;
           newInfo.EntityId = entityId;
           newInfo.Version = version;
           newInfo.TimeStamp = timeStamp;
       }
    }
    
    public static void UpdateVersion(this RemoteGhost self, long entityId, long version, long timeStamp)
    {
        if(self.RemoteGhostInfos.TryGetValue(entityId, out var info))
        {
            info.Version = version;
            info.TimeStamp = timeStamp;
        }
    }
    
    public static void Remove(this RemoteGhost self, long entityId)
    {
        if(self.RemoteGhostInfos.Remove(entityId, out var info))
        {
            ObjectPool.Instance.Recycle(info);
        }
    }

    public static bool TryGetValue(this RemoteGhost self, long entityId, out RemoteGhostInfo info)
    {
        return self.RemoteGhostInfos.TryGetValue(entityId, out info);
    }
}