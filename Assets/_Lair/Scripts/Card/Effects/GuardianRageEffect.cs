using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 수호자의 분노 (Tank A, 구 Berserk 자리 리뉴얼, 기획서 §3.1 #7 / §10.4).
    //# Wisp/Wraith 한정으로 받는 데미지 ×0.5 + HP ×2.0, _duration 초.
    //# 적용 종 ({Wisp, Wraith}) + 배율은 MonsterBuffService.GuardianRage 케이스 내 상수 (§10.1 디자인 단정).
    [Serializable]
    public class GuardianRageEffect : ICardEffect
    {
        [SerializeField] private float _duration = 15f;

        public void Apply(IBattleContext ctx)
            => ctx.AddMonsterBuff(EMonsterBuff.GuardianRage, _duration);
    }
}
