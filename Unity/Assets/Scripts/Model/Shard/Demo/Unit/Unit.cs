namespace Bliss
{
    /// <summary>A player or other unit owned by a GameplayWorld or its LevelScene.</summary>
    public class Unit : GamePlayEntity, IAwake, IMountToScene, IDestroy
    {
        public int UnitConfigId;
    }
}