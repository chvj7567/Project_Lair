using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 근처에 박쥐 _count 마리 스폰.
    [Serializable]
    public class SpawnBatsEffect : ICardEffect
    {
        [SerializeField] private int _count = 5;

        public void Apply(IBattleContext ctx)
        {
            var heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            for (int i = 0; i < _count; ++i)
                ctx.SpawnMonster(EMonster.Bat, heroT.position);
        }
    }
}
