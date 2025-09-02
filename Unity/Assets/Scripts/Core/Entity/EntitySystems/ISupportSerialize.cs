namespace Bliss
{
    public interface ISupportSerialize
    {
        void BeforeSerialize();
        void AfterDeserialize();
    }
}