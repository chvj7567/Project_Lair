namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Tank Tier3 (7장 임계) — 글로벌 필드 캡 +6 (18→24, 영구).
    //# 기획서 §4.2 표·§10.3.
    public class TankSynergyTier3 : IBuildSynergyTier
    {
        private const int CapDelta = 6;

        public void Apply(IBattleContext ctx)
        {
            ctx.IncrementGlobalMonsterCap(CapDelta);
        }
    }
}
