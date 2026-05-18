using System;
using System.Collections.Generic;

namespace ChvjUnityInfra
{
    /// <summary>
    /// 여러 IDisposable을 묶어 한 번에 해제할 수 있는 컨테이너.
    /// UniRx의 CompositeDisposable과 같은 역할이지만 외부 의존성 없음.
    /// </summary>
    public sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Add(IDisposable disposable)
        {
            if (disposable == null) return;
            _disposables.Add(disposable);
        }

        public void Add(Action onDispose)
        {
            if (onDispose == null) return;
            _disposables.Add(new ActionDisposable(onDispose));
        }

        public void Clear()
        {
            for (int i = 0; i < _disposables.Count; i++)
            {
                try { _disposables[i]?.Dispose(); }
                catch { }
            }
            _disposables.Clear();
        }

        public void Dispose() => Clear();

        private class ActionDisposable : IDisposable
        {
            private Action _action;

            public ActionDisposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action?.Invoke();
                _action = null;
            }
        }
    }
}
