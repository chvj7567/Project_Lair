using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 수호자의 분노 (Tank A, 구 Berserk 자리 리뉴얼, 기획서 §3.1 #7 / §10.4).
    //# Wisp/Wraith 한정으로 받는 데미지 ×0.5, _duration 초 (2026-06-01 HP ×2.0 제거 — SO description 정합).
    [Serializable]
    public class GuardianRageEffect : ICardEffect
    {
        [SerializeField] private float _duration = 15f;

        public void Apply(IBattleContext ctx)
            => ctx.AddMonsterBuff(EMonsterBuff.GuardianRage, _duration);
    }
}
