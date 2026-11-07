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

    internal static class SystemUpdateGroupRange
    {
        internal const int FIRST_EARLY_UPDATE = 1;
        internal const int FIRST_UPDATE = (int)ESystemUpdateGroup.EarlyUpdate + 1;
        internal const int FIRST_LATE_UPDATE = (int)ESystemUpdateGroup.Update + 1;
    }
}