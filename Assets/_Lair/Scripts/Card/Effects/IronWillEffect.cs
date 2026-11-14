using Lair.Data;

namespace Lair.Card
{
    //# 강철 의지 — 모든 몬스터 받는 데미지 ↓ (_duration 초). MonsterBuffService 위임.
    public class IronWillEffect : ICardEffect
    {
        private readonly float _duration;

        public IronWillEffect(CardParams p)
        {
            _duration = p.Get("duration");
        }

        public void Apply(IBattleContext ctx)
            => ctx.AddMonsterBuff(EMonsterBuff.IronWill, _duration);
    }
}
