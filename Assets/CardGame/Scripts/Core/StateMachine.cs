using System;

namespace CardGame.Core
{
    /// <summary>
    /// 轻量状态机。战斗流程、主菜单流程或网络房间流程都可以复用它。
    /// 它只负责维护和广播状态，不把业务规则硬编码进状态机。
    /// </summary>
    public sealed class StateMachine<TState> where TState : struct, Enum
    {
        public TState Current { get; private set; }

        public event Action<TState, TState> StateChanged;

        public StateMachine(TState initialState)
        {
            Current = initialState;
        }

        public bool TryTransition(TState nextState)
        {
            if (Equals(Current, nextState))
            {
                return false;
            }

            TState previous = Current;
            Current = nextState;
            StateChanged?.Invoke(previous, nextState);
            return true;
        }
    }
}
