using System;
using Lair.Data;

namespace Lair.Card
{
    //# 지속 스폰 — 리퍼를 출력 중인 모든 Spawner 의 동시 출력 +1 (§3.2 C안).
    [Serializable]
    public class SpawnReapersEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
            => ctx.IncrementSpawnerOutput(EMonster.Reaper);
    }
}
