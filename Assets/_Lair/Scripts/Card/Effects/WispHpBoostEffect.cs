using Lair.Data;

namespace Lair.Card
{
    //# 지속 스폰 — 위스프 종 최대 HP 글로벌 영구 버프 (×_hpMul).
    //# 이후 스폰되는 위스프 전부 + 현재 필드 위스프 전부에 적용 (§3.1).
    public class WispHpBoostEffect : ICardEffect
    {
        private readonly float _hpMul;

        public WispHpBoostEffect(CardParams p)
        {
            _hpMul = p.Get("hpMul");
        }

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, _hpMul);
    }
}
