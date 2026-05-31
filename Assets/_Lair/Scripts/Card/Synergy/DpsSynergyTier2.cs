using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Dps Tier2 (5장 임계) — Reaper+Hex Cooldown ×0.8 (=공속 +25%, 글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class DpsSynergyTier2 : IBuildSynergyTier
    {
        private const float CooldownMul = 0.8f;

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Cooldown, CooldownMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Hex,    EMonsterStatKind.Cooldown, CooldownMul);
        }
    }
}
