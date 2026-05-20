using System;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 IMover.Speed 를 _factor 배로 _duration 초간 감소.
    //# HeroAuraRunner 가 시간 관리 — 만료 시 OnDetached 로 자동 복원.
    [Serializable]
    public class HeroSlowEffect : ICardEffect
    {
        [SerializeField] private float _factor = 0.6f;
        [SerializeField] private float _duration = 5f;

        public void Apply(IBattleContext ctx)
        {
            var mover = ctx.GetHeroMover();
            if (mover == null) return;
            ctx.ApplyHeroAura(new SlowAura(mover, _factor), _duration);
        }
    }
}
