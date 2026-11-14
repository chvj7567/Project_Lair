using Lair.Data;

namespace Lair.Card
{
    //# 지속 스폰 — 레이스 종 최대 HP 글로벌 영구 버프 (×_hpMul).
    //# 카드 리뉴얼 v0.6 (2026-05-31) — 구 효과(Power ×1.5) 폐기, Tank 정체성에 맞춰 HP 강화로 리뉴얼.
    public class WraithDamageBoostEffect : ICardEffect
    {
        private readonly float _hpMul;

        public WraithDamageBoostEffect(CardParams p)
        {
            _hpMul = p.Get("hpMul");
        }

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Hp, _hpMul);
    }
}
