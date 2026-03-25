using System;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    public abstract class GameplayPostLockEvent
    {
        public abstract void ExecuteIfBound();
        
        public static GameplayPostLockEvent Bind<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2,
            T3 arg3, T4 arg4, T5 arg5)
        {
            var gameplayPostLockEvent = new GameplayPostLockEvent<T1, T2, T3, T4, T5>(action, arg1, arg2, arg3, arg4, arg5);
            return gameplayPostLockEvent;
        }
        
        public static GameplayPostLockEvent Bind<T1, T2, T3, T4, T5>(GameplayPostLockEvent_Ref2<T1, T2, T3, T4, T5>.PostLockEvent action, T1 arg1, T2 arg2,
            T3 arg3, T4 arg4, T5 arg5)
        {
            var gameplayPostLockEvent = new GameplayPostLockEvent_Ref2<T1, T2, T3, T4, T5>(action, arg1, arg2, arg3, arg4, arg5);
            return gameplayPostLockEvent;
        }
    }
    
    public class GameplayPostLockEvent<T1, T2, T3, T4, T5> : GameplayPostLockEvent
    {
        private Action<T1, T2, T3, T4, T5> _action;
        private T1 _arg1;
        private T2 _arg2;
        private T3 _arg3;
        private T4 _arg4;
        private T5 _arg5;
        
        public GameplayPostLockEvent(Action<T1, T2, T3, T4, T5> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            _action = action;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
            _arg5 = arg5;
        }
        
        public override void ExecuteIfBound()
        {
            if (_action != null)
            {
                _action(_arg1, _arg2, _arg3, _arg4, _arg5);
            }

            _action = null;
        }
    }
    
    public class GameplayPostLockEvent_Ref2<T1, T2, T3, T4, T5> : GameplayPostLockEvent
    {
        public delegate void PostLockEvent(T1 arg1, ref T2 arg2, ref T3 arg3, T4 arg4, T5 arg5);
        public PostLockEvent _action;
        private T1 _arg1;
        private T2 _arg2;
        private T3 _arg3;
        private T4 _arg4;
        private T5 _arg5;
        
        public GameplayPostLockEvent_Ref2(PostLockEvent action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            _action = action;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
            _arg5 = arg5;
        }
        
        public override void ExecuteIfBound()
        {
            if (_action != null)
            {
                _action.Invoke(_arg1, ref _arg2, ref _arg3, _arg4, _arg5);
            }

            _action = null;
        }
    }
}