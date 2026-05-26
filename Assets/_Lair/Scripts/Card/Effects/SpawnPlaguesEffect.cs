using System;
using Lair.Data;

namespace Lair.Card
{
    //# 지속 스폰 — 플레이그를 출력 중인 모든 Spawner 의 동시 출력 +1 (§3.2 C안).
    //# 스타터 프리셋에는 플레이그 Spawner 가 없어 no-op (의도된 죽은 픽, §5.5).
    [Serializable]
    public class SpawnPlaguesEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
            => ctx.IncrementSpawnerOutput(EMonster.Plague);
    }
}
