namespace Bliss
{
    public enum ESystemUpdateGroup
    {
        None = -1,
        EarlyUpdate = 100,
        Update = 200,
        LateUpdate = 300,
        Max = 301,
    }

    public static class SystemUpdateGroupRange
    {
        public const int FIRST_EARLY_UPDATE = 1;
        public const int FIRST_UPDATE = (int)ESystemUpdateGroup.EarlyUpdate + 1;
        public const int FIRST_LATE_UPDATE = (int)ESystemUpdateGroup.Update + 1;
    }
}