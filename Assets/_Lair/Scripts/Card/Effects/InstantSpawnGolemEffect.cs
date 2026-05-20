using System;
using Lair.Data;

namespace Lair.Card
{
    //# 영웅 위치 근처에 골렘 1마리 즉시 소환. ctx.SpawnMonster 가 CHMPool 사용 (Rule 12).
    [Serializable]
    public class InstantSpawnGolemEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
        {
            var heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            ctx.SpawnMonster(EMonster.Golem, heroT.position);
        }
    }
}
