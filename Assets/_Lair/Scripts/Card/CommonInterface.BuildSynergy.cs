namespace Lair.Card
{
    //# Rule 02 §9 — 도메인별 공용 인터페이스 단일 파일. 분할 prefix: CommonInterface.BuildSynergy.
    //# 카드 리뉴얼 v0.6 — 빌드 시너지 Tier 효과 (Layer 1).
    public interface IBuildSynergyTier
    {
        void Apply(IBattleContext ctx);
    }
}
