using System.Collections.Generic;

namespace Bliss;

public class GateSessionKeyComponent : Entity
{
    /// <summary>
    /// key: token, value: player account
    /// </summary>
    public Dictionary<long, string> SessionTokens = new();
}