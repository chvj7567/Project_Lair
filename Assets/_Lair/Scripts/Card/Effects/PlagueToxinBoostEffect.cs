using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 "역병의 손길" (ID PlagueSlowBoost 유지) — 역병귀 독 피해 배율 글로벌 영구 버프 (PoisonDamageMul x_toxinMul, monster-growth-currencies §15.0).
    [Serializable]
    public class PlagueToxinBoostEffect : ICardEffect
    {
        [SerializeField] private float _toxinMul = 1.4f;

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.PoisonDamage, _toxinMul);
    }
}
