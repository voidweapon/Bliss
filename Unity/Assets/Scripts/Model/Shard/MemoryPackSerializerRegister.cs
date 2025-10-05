namespace Bliss
{
    [SerializePolymorphismRegister]
    public static partial class MemoryPackSerializerRegister
    {
        static partial void Register();

        public static void Init()
        {
            Register();
        }
    }
}