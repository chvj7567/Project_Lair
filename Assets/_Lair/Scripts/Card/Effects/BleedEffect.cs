using Lair.Data;

namespace Lair.Card
{
    //# 출혈 — 부착 후 _duration 초간 1초당 HP -_ratio% (이동/정지 무관).
    public class BleedEffect : ICardEffect
    {
        private readonly float _ratio;
        private readonly float _duration;

        public BleedEffect(CardParams p)
        {
            _ratio = p.Get("ratio");
            _duration = p.Get("duration");
        }

        public void Apply(IBattleContext ctx)
        {
            ctx.ApplyHeroAura(new BleedAura(_ratio), _duration);
        }
    }
}
