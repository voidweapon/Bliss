namespace Bliss
{
    public static class GameplayLayerDefine
    {
        public const int Default = 0;
        public const int Ground = 6;
        public const int Unit = 7;
        public const int Bullet = 8;
    }

    public static class GameplayLayerMask
    {
        public const int None = 0;
        public const int All = -1;

        public const int DefaultMask = 1 << GameplayLayerDefine.Default;
        public const int UnitMask = 1 << GameplayLayerDefine.Unit;
        public const int WalkableMask = 1 << GameplayLayerDefine.Default | 1 << GameplayLayerDefine.Ground;
        public const int CharacterCollidableMask = 1 << GameplayLayerDefine.Default | 1 << GameplayLayerDefine.Ground;
    }
}