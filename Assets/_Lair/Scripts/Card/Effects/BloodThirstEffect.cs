using System;
using UnityEngine;

namespace Lair.Card
{
    //# 피의 갈증 — _duration 초간 몬스터 처치 시 주변 몬스터 회복. BloodThirstService 위임.
    [Serializable]
    public class BloodThirstEffect : ICardEffect
    {
        [SerializeField] private float _duration = 30f;

        public void Apply(IBattleContext ctx)
            => ctx.ActivateBloodThirst(_duration);
    }
}
