using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Dps Tier1 (3장 임계) — Reaper+Hex Power ×1.3 (글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class DpsSynergyTier1 : IBuildSynergyTier
    {
        private const float PowerMul = 1.3f;

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Power, PowerMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Hex,    EMonsterStatKind.Power, PowerMul);
        }
    }
}
