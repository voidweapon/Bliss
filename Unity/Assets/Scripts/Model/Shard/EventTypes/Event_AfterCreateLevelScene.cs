namespace Bliss
{
    /// <summary>
    /// Marks the beginning of a LevelScene transition flow.
    /// </summary>
    public struct Event_StartChangeLevelScene
    {
        
    }
    
    /// <summary>
    /// Marks the end of a LevelScene transition flow.
    /// </summary>
    public struct Event_EndChangeLevelScene
    {
        
    }

    
    /// <summary>
    /// Requests preparation of the required resources for a LevelScene.
    /// </summary>
    public struct Event_AfterCreateLevelScene
    {
        /// <summary>
        /// The Level configuration identifier to prepare.
        /// </summary>
        public int LevelConfigId;
    }

    /// <summary>
    /// Requests activation of the runtime behavior for a prepared LevelScene.
    /// </summary>
    public struct Event_EnableLevelScene
    {
        /// <summary>
        /// Optional local Unit whose runtime components are added during the root scene's enable phase.
        /// </summary>
        public Entity LocalUnit;
    }

    /// <summary>
    /// Requests stopping runtime behavior before a failed LevelScene tree is destroyed.
    /// </summary>
    public struct Event_StopEnableLevelScene
    {
    }

    /// <summary>
    /// Requests post-enable cleanup that does not change the enable result.
    /// </summary>
    public struct Event_AfterEnableLevelScene
    {
    }
    
    /// <summary>
    /// Requests creation of client scene entities for a LevelScene.
    /// </summary>
    public struct Event_EndCreateLevelScene
    {
        /// <summary>
        /// The Level configuration identifier used to create client scene entities.
        /// </summary>
        public int LevelConfigId;
    }
}