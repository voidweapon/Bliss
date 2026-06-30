using System.Collections.Generic;

namespace Bliss;

public class PlayerManagerComponent : Entity
{
    public Dictionary<string, EntityRef<Player>> dictionary = new Dictionary<string, EntityRef<Player>>();

}