using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 지속 스폰 — 골렘 종 공격력 글로벌 영구 버프 (×_mul).
    [Serializable]
    public class GolemDamageBoostEffect : ICardEffect
    {
        [SerializeField] private float _mul = 1.5f;

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Golem, EMonsterStatKind.Power, _mul);
    }
}
