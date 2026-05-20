using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 폭주 — 모든 몬스터 HP 즉시 -50% + 데미지 ↑ (_duration 초).
    [Serializable]
    public class BerserkEffect : ICardEffect
    {
        [SerializeField] private float _duration = 15f;

        public void Apply(IBattleContext ctx)
        {
            ctx.HalveAllMonsterHp();
            ctx.AddMonsterBuff(EMonsterBuff.BerserkPower, _duration);
        }
    }
}
