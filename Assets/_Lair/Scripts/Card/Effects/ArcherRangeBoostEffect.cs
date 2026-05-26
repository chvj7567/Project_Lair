using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 지속 스폰 — 궁수 종 사거리 글로벌 영구 버프 (×_rangeMul).
    [Serializable]
    public class ArcherRangeBoostEffect : ICardEffect
    {
        [SerializeField] private float _rangeMul = 1.4f;

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Archer, EMonsterStatKind.Range, _rangeMul);
    }
}
