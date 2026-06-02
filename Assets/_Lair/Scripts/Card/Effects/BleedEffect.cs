using System;
using UnityEngine;

namespace Lair.Card
{
    //# 출혈 — 부착 후 _duration 초간 1초당 HP -_ratio% (이동/정지 무관).
    [Serializable]
    public class BleedEffect : ICardEffect
    {
        [SerializeField] private float _ratio = 0.02f;
        [SerializeField] private float _duration = 10f;

        public void Apply(IBattleContext ctx)
        {
            ctx.ApplyHeroAura(new BleedAura(_ratio), _duration);
        }
    }
}
