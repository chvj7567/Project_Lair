using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 빠른 번식 (Swarm A, 구 SwarmRush 자리 — 효과 교체, ECardId.Multiply 값명·SO 파일명 보존).
    //# 영구 효과: 팬텀 스포너 주기 ×_periodMul 영구 (BattleController.ScaleSpawnerPeriodForType).
    [Serializable]
    public class FastBreedingEffect : ICardEffect
    {
        [SerializeField] private float _periodMul = 0.6f;

        public void Apply(IBattleContext ctx)
        {
            ctx.ScaleSpawnerPeriodForType(EMonster.Phantom, _periodMul);
        }
    }
}
