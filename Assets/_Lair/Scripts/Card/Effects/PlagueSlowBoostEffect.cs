using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 지속 스폰 — 플레이그 종 둔화 배율 글로벌 영구 버프 (SlowFactorMul ×_slowFactor).
    //# _slowFactor 는 치환값이 아니라 배율 — 1픽 시 BaseSlowFactor 0.8 × 0.75 = 0.6 (§3.0.1).
    //# 낮을수록 강한 둔화. SO 직렬화 값은 0.75 (PlagueSlowBoost.asset 재저장, §7.5.10).
    [Serializable]
    public class PlagueSlowBoostEffect : ICardEffect
    {
        [SerializeField] private float _slowFactor = 0.75f;

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.SlowFactor, _slowFactor);
    }
}
