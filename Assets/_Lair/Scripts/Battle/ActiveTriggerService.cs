using System;

namespace Lair.Battle
{
    //# BattleClock.OnTick 구독 → 임계점 N개 통과 1회 감지.
    //# 기본: {30,90,150,210,270}초 (5개). 디버그/튜닝용으로 생성자에 다른 배열 주입 가능.
    public class ActiveTriggerService : IDisposable
    {
        //# 기본 임계점 — 분단위(60/120/180/240) 제거 (spec §2.B). {30,90,150,210,270} 총 5개.
        private static readonly float[] DefaultThresholds =
            { 30f, 90f, 150f, 210f, 270f };

        private readonly float[] _thresholds;
        private readonly bool[] _fired;
        private readonly BattleClock _clock;

        public event Action<int> OnTriggered;   //# 0..N-1, 임계점 인덱스

        //# thresholds 미지정 시 {30,90,150,210,270} 5개 사용.
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
