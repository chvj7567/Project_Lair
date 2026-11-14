using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 무력화 — 영웅 공격력 _duration 초간 ×_factor.
    public class WeakenEffect : ICardEffect
    {
        private readonly float _factor;
        private readonly float _duration;

        public WeakenEffect(CardParams p)
        {
            _factor = p.Get("factor");
            _duration = p.Get("duration");
        }

        public void Apply(IBattleContext ctx)
        {
            Transform heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            LairCharacter character = heroT.GetComponent<LairCharacter>();
            IAttacker atk = character != null ? character.Get<IAttacker>() : null;
            if (atk == null) return;
            ctx.ApplyHeroAura(new WeakenAura(atk, _factor), _duration);
        }
    }
}
