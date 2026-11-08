namespace Lair.Character
{
    //# 등급 이펙트 전장 동시 활성 상한(monster-grades §9.3 시작값) — 먼저 나온 몬스터 우선, 초과 시 새 몬스터는 그 이펙트 생략, 반환 시 슬롯 반환.
    //# 전투 씬 한 번에 한 세트라 정적 카운터 — 씬 종료 때 Reset 한다(BattleController).
    public static class GradeFxBudget
    {
        public const int AuraCap = 40;
        public const int TornadoCap = 24;
        public const int SunburstCap = 8;

        private static int _aura;
        private static int _tornado;
        private static int _sunburst;

        public static bool TryAcquireAura() => TryAcquire(ref _aura, AuraCap);
        public static bool TryAcquireTornado() => TryAcquire(ref _tornado, TornadoCap);
        public static bool TryAcquireSunburst() => TryAcquire(ref _sunburst, SunburstCap);

        public static void ReleaseAura() => Release(ref _aura);
        public static void ReleaseTornado() => Release(ref _tornado);
        public static void ReleaseSunburst() => Release(ref _sunburst);

        public static void Reset()
        {
            _aura = 0;
            _tornado = 0;
            _sunburst = 0;
        }

        private static bool TryAcquire(ref int counter, int cap)
        {
            if (counter >= cap)
                return false;
            counter++;
            return true;
        }

        private static void Release(ref int counter)
        {
            if (counter > 0)
            {
                counter--;
            }
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public static int AuraCountForTest => _aura;
#endif
    }
}
