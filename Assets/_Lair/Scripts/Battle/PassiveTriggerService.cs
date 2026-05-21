using System;
using Lair.Character;

namespace Lair.Battle
{
    //# Hero IHealth 의 OnChanged 를 구독해 HP 임계점 통과 1회 감지.
    //# 기본: 90%..10% (9개). 디버그/튜닝용으로 생성자에 다른 배열 주입 가능.
    public class PassiveTriggerService : IDisposable
    {
        //# 기본 임계점 — 90%, 80%, ..., 10%
        private static readonly float[] DefaultThresholds =
            { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

        private readonly float[] _thresholds;
        private readonly bool[] _fired;
        private readonly IHealth _hero;

        public event Action<int> OnTriggered;   //# 0=첫 임계점, ...

        //# thresholds 미지정 시 90%..10% 9개 사용.
        public PassiveTriggerService(IHealth hero, float[] thresholds = null)
        {
            _thresholds = thresholds ?? DefaultThresholds;
            _fired = new bool[_thresholds.Length];
            _hero = hero;
            _hero.OnChanged += HandleChanged;
        }

        public void Dispose()
        {
            if (_hero != null) _hero.OnChanged -= HandleChanged;
        }

        private void HandleChanged(int current, int max)
        {
            if (max <= 0) return;
            float ratio = (float)current / max;
            for (int i = 0; i < _thresholds.Length; ++i)
            {
                if (_fired[i]) continue;
                if (ratio <= _thresholds[i])
                {
                    _fired[i] = true;
                    OnTriggered?.Invoke(i);
                }
            }
        }
    }
}
