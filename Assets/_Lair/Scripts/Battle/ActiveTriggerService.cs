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

        //# 카드 선택 부제용 — 액티브 트리거 주기(초). 임계점이 2개 이상이면 첫 두 임계점의 간격, 1개면 그 값, 없으면 0.
        //# 기본 {30,90,...} 는 간격 60 이다 — 첫 트리거(30초)와 주기(60초)가 다르므로 표기는 실제 간격을 따른다.
        public static int ResolvePeriodSeconds(float[] thresholds)
        {
            float[] source = thresholds ?? DefaultThresholds;
            if (source.Length == 0)
                return 0;
            if (source.Length == 1)
                return (int)Math.Round(source[0]);
            return (int)Math.Round(source[1] - source[0]);
        }

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
