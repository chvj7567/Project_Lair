using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 지속 스폰 — 슬라임 종 최대 HP 글로벌 영구 버프 (×_hpMul).
    //# 이후 스폰되는 슬라임 전부 + 현재 필드 슬라임 전부에 적용 (§3.1).
    [Serializable]
    public class SlimeHpBoostEffect : ICardEffect
    {
        [SerializeField] private float _hpMul = 1.5f;

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Slime, EMonsterStatKind.Hp, _hpMul);
    }
}
