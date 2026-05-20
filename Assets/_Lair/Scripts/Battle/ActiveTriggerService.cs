using System;

namespace Lair.Battle
{
    //# BattleClock.OnTick 구독 → 임계점 N개 통과 1회 감지.
    //# 기본: 30/60/.../270초 (9개). 디버그/튜닝용으로 생성자에 다른 배열 주입 가능.
    public class ActiveTriggerService : IDisposable
    {
        //# 기본 임계점 — 30, 60, ..., 270 (초) 총 9개
        private static readonly float[] DefaultThresholds =
            { 30f, 60f, 90f, 120f, 150f, 180f, 210f, 240f, 270f };

        private readonly float[] _thresholds;
        private readonly bool[] _fired;
        private readonly BattleClock _clock;

        public event Action<int> OnTriggered;   //# 0..N-1, 임계점 인덱스

        //# thresholds 미지정 시 30초 단위 9개 사용.
        public ActiveTriggerService(BattleClock clock, float[] thresholds = null)
        {
            _thresholds = thresholds ?? DefaultThresholds;
            _fired = new bool[_thresholds.Length];
            _clock = clock;
            if (_clock != null) _clock.OnTick += HandleTick;
        }

        public void Dispose()
        {
            if (_clock != null) _clock.OnTick -= HandleTick;
        }

        private void HandleTick(float elapsed)
        {
            for (int i = 0; i < _thresholds.Length; ++i)
            {
                if (_fired[i]) continue;
                if (elapsed >= _thresholds[i])
                {
                    _fired[i] = true;
                    OnTriggered?.Invoke(i);
                }
            }
        }
    }
}
