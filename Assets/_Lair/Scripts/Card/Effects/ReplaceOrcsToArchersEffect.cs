using System;
using Lair.Data;

namespace Lair.Card
{
    //# 지속 스폰 — 출력 종이 오크인 모든 Spawner 의 출력 종을 궁수로 영구 변경 (§3.4).
    //# 필드 몬스터 즉살 없음 — 앞으로의 출력만 바꾼다. 매칭 Spawner 0개면 no-op.
    [Serializable]
    public class ReplaceOrcsToArchersEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
            => ctx.ReplaceSpawnerOutput(EMonster.Orc, EMonster.Archer);
    }
}
