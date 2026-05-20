using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 강철 의지 — 모든 몬스터 받는 데미지 ↓ (_duration 초). MonsterBuffService 위임.
    [Serializable]
    public class IronWillEffect : ICardEffect
    {
        [SerializeField] private float _duration = 15f;

        public void Apply(IBattleContext ctx)
            => ctx.AddMonsterBuff(EMonsterBuff.IronWill, _duration);
    }
}
