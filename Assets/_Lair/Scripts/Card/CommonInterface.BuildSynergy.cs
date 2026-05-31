namespace Lair.Card
{
    //# Rule 02 §9 — 도메인별 공용 인터페이스 단일 파일. 분할 prefix: CommonInterface.BuildSynergy.
    //# 카드 리뉴얼 v0.6 — 빌드 시너지 Tier 효과 (Layer 1).
    //# 같은 축 N장 픽 임계(3/5/7) 도달 시 BuildSynergyService 가 1회 Apply 호출.
    //# Apply 안에서 IBattleContext.RegisterMonsterTypeBuff / AddMonsterBuff /
    //# IncrementGlobalMonsterCap / ScaleAllSpawnerPeriods / IncrementAllSpawnerOutputs 등 호출.
    //# 구체 구현은 Phase 2 Task 11 에서 12개 (4축 × 3Tier) 작성.
    public interface IBuildSynergyTier
    {
        void Apply(IBattleContext ctx);
    }
}
