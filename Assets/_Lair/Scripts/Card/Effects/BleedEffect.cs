using System;
using UnityEngine;

namespace Lair.Card
{
    //# 출혈 — 영웅 이동 중일 때 _duration 초간 1초당 HP -_ratio%.
    [Serializable]
    public class BleedEffect : ICardEffect
    {
        [SerializeField] private float _ratio = 0.02f;
        [SerializeField] private float _duration = 10f;

        public void Apply(IBattleContext ctx)
        {
            var mover = ctx.GetHeroMover();
            if (mover == null) return;
            ctx.ApplyHeroAura(new BleedAura(mover, _ratio), _duration);
        }
    }
}
