using System;
using Lair.Character;
using UnityEngine;

namespace Lair.Card
{
    //# 무력화 — 영웅 공격력 _duration 초간 ×_factor.
    [Serializable]
    public class WeakenEffect : ICardEffect
    {
        [SerializeField] private float _factor = 0.5f;
        [SerializeField] private float _duration = 10f;

        public void Apply(IBattleContext ctx)
        {
            Transform heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            IAttacker atk = heroT.GetComponent<IAttacker>();
            if (atk == null) return;
            ctx.ApplyHeroAura(new WeakenAura(atk, _factor), _duration);
        }
    }
}
