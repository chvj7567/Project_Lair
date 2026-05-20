using System;
using UnityEngine;

namespace Lair.Card
{
    //# 둔화 — 영웅 이동속도 _duration 초간 ×_factor. (B2 HeroSlowEffect 대체)
    [Serializable]
    public class SlowEffect : ICardEffect
    {
        [SerializeField] private float _factor = 0.5f;
        [SerializeField] private float _duration = 10f;

        public void Apply(IBattleContext ctx)
        {
            var mover = ctx.GetHeroMover();
            if (mover == null) return;
            ctx.ApplyHeroAura(new SlowAura(mover, _factor), _duration);
        }
    }
}
