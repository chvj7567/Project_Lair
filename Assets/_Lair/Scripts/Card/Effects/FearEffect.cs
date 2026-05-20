using System;
using Lair.Character;
using UnityEngine;

namespace Lair.Card
{
    //# 공포 — 영웅 _duration 초간 도주.
    [Serializable]
    public class FearEffect : ICardEffect
    {
        [SerializeField] private float _duration = 3f;

        public void Apply(IBattleContext ctx)
        {
            var heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            var ai = heroT.GetComponent<AutoCombatAI>();
            if (ai == null) return;
            ctx.ApplyHeroAura(new FearAura(ai), _duration);
        }
    }
}
