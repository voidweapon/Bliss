using Bliss.Core;

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
    }

    public static void UpdateClientVersion(this RemoteGhost self, long version)
    {
        self.ClientVersion = version;
    }
}