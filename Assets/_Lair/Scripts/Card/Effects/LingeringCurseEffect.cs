using System;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 "끈질긴 저주" (monster-growth-currencies §15.3) — 영웅에게 걸리는 Debuff 축 시한 디버프(독·공포·출혈·무력화) 지속 +25%/픽(가산).
    //# 영구 디버프와 Swarm Slow 둔화는 대상 아님 — 러너의 Debuff 축 시한 보정 지점에서 신앙의 가호 x0.6 과 곱으로 합성된다.
    [Serializable]
    public class LingeringCurseEffect : ICardEffect
    {
        [SerializeField] private float _durationBonusPerPick = 0.25f;

        public void Apply(IBattleContext ctx) => ctx.AddHeroDebuffDurationBonus(_durationBonusPerPick);
    }
}
