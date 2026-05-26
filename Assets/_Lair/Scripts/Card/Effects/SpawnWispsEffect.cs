using System;
using Lair.Data;

namespace Lair.Card
{
    //# 지속 스폰 — 위스프를 출력 중인 모든 Spawner 의 동시 출력 +1 (§3.2 C안).
    //# 위스프 Spawner 가 0개면 no-op.
    [Serializable]
    public class SpawnWispsEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
            => ctx.IncrementSpawnerOutput(EMonster.Wisp);
    }
}
