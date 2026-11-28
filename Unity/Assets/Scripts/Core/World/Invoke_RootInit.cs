using ET;

namespace Bliss
{
    public struct Invoke_RootInit
    {
        /// <summary>The Root being initialized.</summary>
        public Root Root;

        /// <summary>The optional cooperative cancellation source for initialization.</summary>
        public ETCancellationToken CancellationToken;

        /// <summary>Throws when the initialization cancellation source has been canceled.</summary>
        /// <exception cref="TaskAbortedException">The initialization was canceled.</exception>
        public void ThrowIfCancellationRequested()
        {
            if (CancellationToken?.IsCancel() == true)
            {
                throw new TaskAbortedException("Root initialization was canceled.", CancellationToken);
            }
        }
    }
}