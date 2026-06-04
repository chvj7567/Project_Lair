using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 부착돼 있는 동안 항상 1초당 Max×_ratio 데미지 (이동/정지 무관).
    [Serializable]
    public class BleedAura : IHeroAura, IStatusVisual
    {
        public ECardId IconCardId => ECardId.Bleed;

        private readonly float _ratio;   //# 0.02 = 2%
        private float _acc;

        public BleedAura(float ratio = 0.02f)
        {
            _ratio = ratio;
        }

        public void OnAttached(IHealth hero) { _acc = 0f; }

        //# 부착 중 매 1초 Max×_ratio 데미지 — 영웅 이동 여부와 무관.
        public void Tick(IHealth hero, float dt)
        {
            if (hero == null) return;
            _acc += dt;
            while (_acc >= 1f)
            {
                _acc -= 1f;
                //# 데미지 숫자 색 스탬프 — TakeDamage(OnChanged 동기 발행) 직전.
                StampColor(hero, HitFeedbackPalette.Bleed);
                //# RoundToInt — float 정밀도로 (int) 캐스팅이 2.0→1 로 깎이는 것 방지.
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
