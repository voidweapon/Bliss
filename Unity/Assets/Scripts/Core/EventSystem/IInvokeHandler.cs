using System;

namespace Bliss
{
    public interface IInvokeHandler
    {
        Type ArgumentType { get; }
    }

    public abstract class AInvokeHandler<TArg> : IInvokeHandler where TArg : struct
    {
        public Type ArgumentType => typeof(TArg);
        
        public abstract void Handle(TArg args);
    }
    
    public abstract class AInvokeHandler<TArg, TResult> : IInvokeHandler where TArg : struct
    {
        public Type ArgumentType => typeof(TArg);
        
        public abstract TResult Handle(TArg args);
    }
}