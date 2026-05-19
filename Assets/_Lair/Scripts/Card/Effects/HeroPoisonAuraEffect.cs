using System;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 발 밑에 독장판 배치 — 영웅이 그 영역 안에 머무는 동안만 1초마다 _dps 데미지.
    //# 지속 중 같은 카드 재부착 시 HeroAuraRunner 가 기존 Remain 에 _duration 만큼 연장.
    [Serializable]
    public class HeroPoisonAuraEffect : ICardEffect
    {
        [SerializeField] private float _dps = 5f;
        [SerializeField] private float _duration = 5f;
        [SerializeField] private float _radius = 1.25f;   //# Visual prefab scale (2.5) / 2

        public void Apply(IBattleContext ctx)
        {
            ctx.ApplyHeroAura(new PoisonAura(_dps, _radius), durationSeconds: _duration);
        }
    }
}
