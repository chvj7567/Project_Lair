using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 지속 스폰 — 팬텀 종 이동속도 글로벌 영구 버프 (×_speedMul).
    [Serializable]
    public class PhantomMoveSpeedBoostEffect : ICardEffect
    {
        [SerializeField] private float _speedMul = 1.5f;

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Phantom, EMonsterStatKind.MoveSpeed, _speedMul);
    }
}
