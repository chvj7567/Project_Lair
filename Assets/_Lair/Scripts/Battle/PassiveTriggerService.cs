using System;
using Lair.Character;

namespace Lair.Battle
{
    //# Hero IHealth 의 OnChanged 를 구독해 HP 10% 임계점 통과 1회 감지.
    //# 큰 데미지로 여러 임계점 한 번에 통과해도 각각 순차 발동.
    public class PassiveTriggerService : IDisposable
    {
        //# 90%, 80%, ..., 10% — 총 9개
        private static readonly float[] Thresholds =
            { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

        private readonly bool[] _fired = new bool[Thresholds.Length];
        private readonly IHealth _hero;

        public event Action<int> OnTriggered;   //# 0=90%, 1=80%, ..., 8=10%

        public PassiveTriggerService(IHealth hero)
        {
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
            for (int i = 0; i < Thresholds.Length; ++i)
            {
                if (_fired[i]) continue;
                if (ratio <= Thresholds[i])
                {
                    _fired[i] = true;
                    OnTriggered?.Invoke(i);
                }
            }
        }
    }
}
