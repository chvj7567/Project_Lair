using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — Debuff Tier3 시너지 효과 (7장 임계).
    //# BleedAura 의 라운드 끝까지 무제한 변형. ratio 0.01 (1%/s) — 영웅 이동 시에만 발동.
    [Serializable]
    public class EternalBleedAura : IHeroAura, IStatusVisual
    {
        //# 전용 카드 없음 — 동일 "출혈" 능력 아이콘 재사용 (기획서 §2).
        public ECardId IconCardId => ECardId.Bleed;

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
                //# 데미지 숫자 색 스탬프 — 출혈 변형이므로 BleedAura 와 동일 자홍색(기획서 §1.6).
                StampColor(hero, HitFeedbackPalette.Bleed);
                hero.TakeDamage(Mathf.RoundToInt(hero.Max * _ratio));
            }
        }

        public void OnDetached(IHealth hero) { }

        //# DoT 데미지 숫자 색 스탬프 — 피격자의 IDamageColorSink 로 전달.
        private static void StampColor(IHealth hero, Color c)
        {
            if (hero is Component comp && comp != null)
                comp.GetComponent<IDamageColorSink>()?.StampDamageColor(c);
        }
    }
}
