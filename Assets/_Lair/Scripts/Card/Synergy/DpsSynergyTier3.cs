using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Dps Tier3 (7장 임계) — Reaper+Hex Range ×1.3 (글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class DpsSynergyTier3 : IBuildSynergyTier
    {
        private const float RangeMul = 1.3f;

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Range, RangeMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Hex,    EMonsterStatKind.Range, RangeMul);
        }
    }
}
