namespace Bliss
{
    public static class PhysicalInvokeTypes
    {
        public const int Physical_Scene_Query = 100;
        public const int Physical_Scene_Awake = 200;
        public const int Physical_Scene_Destroy = 201;
        /// <summary>Validates that client view resources survive a LevelScene migration.</summary>
        public const int GAMEPLAY_SUBTREE_MIGRATION_VALIDATE = 202;
    }
}