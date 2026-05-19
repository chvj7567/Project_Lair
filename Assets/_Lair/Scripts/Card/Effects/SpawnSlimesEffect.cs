using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 근처에 슬라임 _count 마리 스폰.
    [Serializable]
    public class SpawnSlimesEffect : ICardEffect
    {
        [SerializeField] private int _count = 3;

        public void Apply(IBattleContext ctx)
        {
            var heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            for (int i = 0; i < _count; ++i)
                ctx.SpawnMonster(EMonster.Slime, heroT.position);
        }
    }
}
