using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 patch — 처형 명령 (Dps P, 구 ReplaceReapersToHex 자리 — 효과 교체, ECardId 값명·SO 파일명 보존).
    //# 영구 글로벌 강화: 리퍼·헥스 Power ×_powerMul 곱연산 누적.
    public class ReaperHexPowerBoostEffect : ICardEffect
    {
        private readonly float _powerMul;

        public ReaperHexPowerBoostEffect(CardParams p)
        {
            _powerMul = p.Get("powerMul");
        }

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Power, _powerMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Hex,    EMonsterStatKind.Power, _powerMul);
        }
    }
}
