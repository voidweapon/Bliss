namespace Bliss
{
    public struct Event_StartChangeLevelScene
    {
        
    }
    
    public struct Event_EndChangeLevelScene
    {
        
    }

    
    public struct Event_AfterCreateLevelScene
    {
        public int LevelConfigId;
    }
    
    public struct Event_EndCreateLevelScene
    {
        public int LevelConfigId;
    }
}