using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — Debuff Tier3 시너지 효과 (7장 임계).
    //# BleedAura 의 라운드 끝까지 무제한 변형. ratio 0.01 (1%/s) — 영웅 이동 시에만 발동.
    //# 기존 BleedAura 와 동일 패턴 (IMover 주입 + IsMoving 조건) — 라이프사이클은 HeroAuraRunner.duration<0.
    [Serializable]
    public class EternalBleedAura : IHeroAura, IStatusVisual
    {
        public EVisual VisualKey => EVisual.BleedStatus;
        public Vector3 Offset => new Vector3(0.4f, 0.05f, 0f);

        private readonly IMover _mover;
        private readonly float _ratio;
        private float _acc;

        public EternalBleedAura(IMover mover, float ratio = 0.01f)
        {
            _mover = mover;
            _ratio = ratio;
        }

        public void OnAttached(IHealth hero)
        {
            _acc = 0f;
        }

        public void Tick(IHealth hero, float dt)
        {
            if (hero == null || _mover == null || _mover.IsMoving == false) return;
            _acc += dt;
            while (_acc >= 1f)
            {
                _acc -= 1f;
                hero.TakeDamage(Mathf.RoundToInt(hero.Max * _ratio));
            }
        }

        public void OnDetached(IHealth hero) { }
    }
}
