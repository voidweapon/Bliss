namespace Bliss;

/// <summary>
/// Tracks server-owned shared state created while enabling a LevelScene.
/// </summary>
public class LevelSceneServerEnableStateComponent : Entity
{
    /// <summary>
    /// The DemoGameMode created by this LevelScene enable attempt, if any.
    /// </summary>
    public DemoGameMode CreatedDemoGameMode { get; set; }
}
