using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 시간 정지 — 영웅 이동·공격 _duration 초간 완전 정지.
    public class TimeStopEffect : ICardEffect
    {
        private readonly float _duration;

        public TimeStopEffect(CardParams p)
        {
            _duration = p.Get("duration");
        }

        public void Apply(IBattleContext ctx)
        {
            Transform heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            LairCharacter character = heroT.GetComponent<LairCharacter>();
            IMover mover = character != null ? character.Get<IMover>() : null;
            IAttacker atk = character != null ? character.Get<IAttacker>() : null;
            ctx.ApplyHeroAura(new TimeStopAura(mover, atk), _duration);
        }
    }
}
