using System;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 던전의 박동 (Swarm P 신규, 기획서 §3.4 #4 / §10.4).
    //# 모든 Spawner 의 _spawnPeriod 에 _periodMul 곱연산 (영구).
    //# 곱연산 누적 (2픽 = ×0.64, 3픽 = ×0.512) — 같은 카드 다중 픽 시 IBattleContext 표면이 그대로 재호출.
    [Serializable]
    public class SpawnerHasteEffect : ICardEffect
    {
        [SerializeField] private float _periodMul = 0.8f;

        public void Apply(IBattleContext ctx)
            => ctx.ScaleAllSpawnerPeriods(_periodMul);
    }
}
