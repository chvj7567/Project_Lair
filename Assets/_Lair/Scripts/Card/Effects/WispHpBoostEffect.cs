using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 지속 스폰 — 위스프 종 최대 HP 글로벌 영구 버프 (×_hpMul).
    //# 이후 스폰되는 위스프 전부 + 현재 필드 위스프 전부에 적용 (§3.1).
    [Serializable]
    public class WispHpBoostEffect : ICardEffect
    {
        [SerializeField] private float _hpMul = 1.5f;

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, _hpMul);
    }
}
