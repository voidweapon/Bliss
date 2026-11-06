using System.Collections.Generic;

namespace Bliss;

/// <summary>
/// 本地和远端的玩家的同步组件
/// 
/// </summary>
public class RemoteGhost : Entity, IAwake, IDestroy
{
    /// <summary>
    /// 本地对象版本
    /// </summary>
    public long Version;
    
    /// <summary>
    /// 远端对象版本
    /// </summary>
    public long ClientVersion;
    
    public Dictionary<long, RemoteGhostInfo> RemoteGhostInfos = new Dictionary<long, RemoteGhostInfo>();
}

public class RemoteGhostInfo
{
    public long EntityId;
    public long Version;
    public float TimeStamp;
}