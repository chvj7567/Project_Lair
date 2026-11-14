using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 죽음의 표식 (Dps A 신규, 기획서 §3.2 #7 / §10.4).
    //# 영웅에 MarkOfDeathAura 부착 — 지속시간 동안 영웅 받는 데미지 ×_dmgTakenMul.
    public class MarkOfDeathEffect : ICardEffect
    {
        private readonly float _dmgTakenMul;
        private readonly float _duration;

        public MarkOfDeathEffect(CardParams p)
        {
            _dmgTakenMul = p.Get("dmgTakenMul");
            _duration = p.Get("duration");
        }

        public void Apply(IBattleContext ctx)
            => ctx.ApplyHeroAura(new MarkOfDeathAura(_dmgTakenMul), _duration);
    }
}
