using Lair.Data;

namespace Lair.Card
{
    //# 광폭화 — 모든 몬스터 공격속도 ↑ (_duration 초). MonsterBuffService 위임.
    public class FrenzyEffect : ICardEffect
    {
        private readonly float _duration;

        public FrenzyEffect(CardParams p)
        {
            _duration = p.Get("duration");
        }

        public void Apply(IBattleContext ctx)
            => ctx.AddMonsterBuff(EMonsterBuff.Frenzy, _duration);
    }
}
