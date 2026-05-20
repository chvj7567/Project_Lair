using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 근처에 거미 _count 마리 스폰.
    [Serializable]
    public class SpawnSpidersEffect : ICardEffect
    {
        [SerializeField] private int _count = 2;

        public void Apply(IBattleContext ctx)
        {
            var heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            for (int i = 0; i < _count; ++i)
                ctx.SpawnMonster(EMonster.Spider, heroT.position);
        }
    }
}
