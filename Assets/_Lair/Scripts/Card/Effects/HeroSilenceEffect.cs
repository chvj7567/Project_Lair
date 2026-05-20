using System;
using Lair.Character;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 공격 _duration 초간 정지. 영웅 Transform 의 IAttacker 컴포넌트에 적용.
    [Serializable]
    public class HeroSilenceEffect : ICardEffect
    {
        [SerializeField] private float _duration = 5f;

        public void Apply(IBattleContext ctx)
        {
            var heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            var atk = heroT.GetComponent<IAttacker>();
            if (atk == null) return;
            ctx.ApplyHeroAura(new SilenceAura(atk), _duration);
        }
    }
}
