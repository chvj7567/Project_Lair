using System;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 몬스터에 _damage 즉발 데미지.
    //# BattleContext.GetMonsters 가 스냅샷 반환하므로 iteration 중 사망 처리 안전.
    [Serializable]
    public class MonsterAoeDamageEffect : ICardEffect
    {
        [SerializeField] private int _damage = 50;

        public void Apply(IBattleContext ctx)
        {
            foreach (var m in ctx.GetMonsters())
                m.TakeDamage(_damage);
        }
    }
}
