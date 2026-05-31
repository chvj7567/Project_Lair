using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Swarm Tier1 (3장 임계) — Phantom+Wisp MoveSpeed ×1.3 (글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class SwarmSynergyTier1 : IBuildSynergyTier
    {
        private const float MoveMul = 1.3f;

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Phantom, EMonsterStatKind.MoveSpeed, MoveMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Wisp,    EMonsterStatKind.MoveSpeed, MoveMul);
        }
    }
}
