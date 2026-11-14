using Lair.Data;

namespace Lair.Card
{
    //# 피의 갈증 — _duration 초간 몬스터 처치 시 주변 몬스터 회복. BloodThirstService 위임.
    public class BloodThirstEffect : ICardEffect
    {
        private readonly float _duration;

        public BloodThirstEffect(CardParams p)
        {
            _duration = p.Get("duration");
        }

        public void Apply(IBattleContext ctx)
            => ctx.ActivateBloodThirst(_duration);
    }
}
