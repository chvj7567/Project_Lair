using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Debuff Tier1 (3장 임계) — Plague SlowFactor ×0.8 (강한 둔화 추가, 글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class DebuffSynergyTier1 : IBuildSynergyTier
    {
        private const float SlowMul = 0.8f;

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.SlowFactor, SlowMul);
        }
    }
}
