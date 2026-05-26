using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 지속 스폰 — 헥스 종 사거리 글로벌 영구 버프 (×_rangeMul).
    [Serializable]
    public class HexRangeBoostEffect : ICardEffect
    {
        [SerializeField] private float _rangeMul = 1.4f;

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Hex, EMonsterStatKind.Range, _rangeMul);
    }
}
