using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 지속 스폰 — 리퍼 종 공격 쿨다운 글로벌 영구 버프 (×_cdMul, 낮을수록 빠른 공격).
    [Serializable]
    public class ReaperAtkSpeedEffect : ICardEffect
    {
        [SerializeField] private float _cdMul = 0.7f;

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Cooldown, _cdMul);
    }
}
