namespace Bliss.Core.EventSystem
{
    public class InvokeAttribute : BaseAttribute
    {
        public long InvokeHandleType { get; }

        public InvokeAttribute(long handleType)
        {
            InvokeHandleType = handleType;
        }
    }
}