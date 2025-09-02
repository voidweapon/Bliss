namespace Bliss.Timer
{
    public abstract class ATimer<TArg> : AInvokeHandlerHandler<TimerCallback> where TArg : class
    {
        public override void Handle(TimerCallback args)
        {
            Run(args.Userdata as TArg);
        }
        
        protected abstract void Run(TArg t);

    }
}