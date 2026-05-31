using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 patch — 공포의 군세 (Tank P, 구 ReplaceWispsToWraith 자리 — 효과 교체, ECardId 값명·SO 파일명 보존).
    //# 영구 글로벌 강화: 위스프·레이스 Power ×_powerMul 곱연산 누적 (RegisterMonsterTypeBuff 의 dict + 필드 소급 적용).
    [Serializable]
    public class WispWraithPowerBoostEffect : ICardEffect
    {
        [SerializeField] private float _powerMul = 1.3f;

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Wisp,   EMonsterStatKind.Power, _powerMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Power, _powerMul);
        }
    }
}
