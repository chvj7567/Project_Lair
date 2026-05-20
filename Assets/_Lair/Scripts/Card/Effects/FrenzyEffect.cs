using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 광폭화 — 모든 몬스터 공격속도 ↑ (_duration 초). MonsterBuffService 위임.
    [Serializable]
    public class FrenzyEffect : ICardEffect
    {
        [SerializeField] private float _duration = 10f;

        public void Apply(IBattleContext ctx)
            => ctx.AddMonsterBuff(EMonsterBuff.Frenzy, _duration);
    }
}
