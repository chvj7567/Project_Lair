using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 Tank Tier3 (7장 임계) — Wisp·Wraith HP ×1.4 추가 내구 버프 (글로벌 영구).
    //# 구 캡 +6 을 캡 제거에 따라 테마 일관 내구 강화로 교체. 기획서 tank-tier3-renewal.md §2.
    public class TankSynergyTier3 : IBuildSynergyTier
    {
        private const float HpMul = 1.4f;

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Wisp,   EMonsterStatKind.Hp, HpMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Hp, HpMul);
        }
    }
}
