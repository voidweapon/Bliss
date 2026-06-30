using System.Collections.Generic;

namespace Bliss;

public class LevelPlayerSpawnPointMgr : Entity
{
    public List<EntityRef<PlayerSpawnPoint>> PlayerSpawnPoints = new();
}