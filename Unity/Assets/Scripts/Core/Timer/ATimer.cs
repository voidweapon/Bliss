namespace Bliss.Timer
{
    /// <summary>
    /// <seealso cref="Bliss.Core.EventSystem.InvokeAttribute"/>
    /// </summary>
    /// <typeparam name="TArg"></typeparam>
    public abstract class ATimer<TArg> : AInvokeHandler<TimerCallback> where TArg : class
    {
        public override void Handle(TimerCallback args)
        {
            Run(args.Userdata as TArg);
        }
        
        protected abstract void Run(TArg t);

    }
}