using System;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 죽음의 표식 (Dps A 신규, 기획서 §3.2 #7 / §10.4).
    //# 영웅에 MarkOfDeathAura 부착 — 지속시간 동안 영웅 받는 데미지 ×_dmgTakenMul.
    [Serializable]
    public class MarkOfDeathEffect : ICardEffect
    {
        [SerializeField] private float _dmgTakenMul = 1.5f;
        [SerializeField] private float _duration = 5f;

        public void Apply(IBattleContext ctx)
            => ctx.ApplyHeroAura(new MarkOfDeathAura(_dmgTakenMul), _duration);
    }
}
