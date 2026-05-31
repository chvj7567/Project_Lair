using System.Collections.Generic;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 빌드 시너지 코어 (POCO, BattleController 가 보유).
    //# Layer 1: 한 라운드의 축별 픽 카운트를 누적하고, BindTier 로 등록된 임계(3/5/7)
    //# 도달 시점에 1회 Apply 발화.
    //# 라운드 시작 / Restart 시 Reset() 호출 — 픽 카운트만 초기화 (Tier 바인딩은 영구).
    public class BuildSynergyService
    {
        //# 축별 누적 픽 카운트. 같은 카드를 K번 픽하면 K번 누적 (기획서 §9.2).
        private readonly Dictionary<EBuildAxis, int> _counts = new Dictionary<EBuildAxis, int>();

        //# (축, 임계) → Tier 효과 매핑. BattleController 가 부팅 시 12개 (4축 × 3Tier) 바인딩.
        private readonly Dictionary<(EBuildAxis, int), IBuildSynergyTier> _tiers
            = new Dictionary<(EBuildAxis, int), IBuildSynergyTier>();

        //# 카운트만 누적 — 테스트 / UI 카운트 표시 / 시뮬레이션 비-ctx 경로용. Apply 발화 없음.
        public void RegisterPick(EBuildAxis axis)
        {
            int prev;
            _counts.TryGetValue(axis, out prev);
            _counts[axis] = prev + 1;
        }

        //# 픽 발생 시 호출 — 카운트 증가 후 새로 도달한 임계가 있으면 1회 Apply.
        //# spec D6: 임계 도달 시 즉시 발화. 같은 임계는 라운드당 1회만 (next 가 정확히 바인딩된 threshold 일 때만).
        public void RegisterPick(EBuildAxis axis, IBattleContext ctx)
        {
            int prev;
            _counts.TryGetValue(axis, out prev);
            int next = prev + 1;
            _counts[axis] = next;

            IBuildSynergyTier tier;
            if (_tiers.TryGetValue((axis, next), out tier))
            {
                tier.Apply(ctx);
            }
        }

        public int GetCount(EBuildAxis axis)
        {
            int v;
            return _counts.TryGetValue(axis, out v) ? v : 0;
        }

        //# 부팅 시 (BattleController.Start) 한 번 호출. axis × threshold 당 1개 Tier 바인딩.
        //# 같은 키 재호출 시 덮어쓰기 (테스트 한정 패턴).
        public void BindTier(EBuildAxis axis, int threshold, IBuildSynergyTier tier)
        {
            _tiers[(axis, threshold)] = tier;
        }

        //# 라운드 시작 / Restart 시 호출. 픽 카운트만 0 으로. Tier 바인딩은 영구 유지.
        public void Reset()
        {
            _counts.Clear();
        }
    }
}
