using System;
using Lair.Character;
using UnityEngine;

namespace Lair.Card
{
    //# 유물 잿불 낙인 — 영웅이 매초 최대 HP x ratioPerSec 화상 피해 (relics §6.2). PoisonAura 의 1초 누적 틱 패턴, 영역 판정·바닥 비주얼 없음.
    //# Debuff 축 + 영구 감소 보정(IPenaltyScalable) → 챕터 5 성기사 "신앙의 가호" 저항 대상 (EternalBleedAura 와 동일 규칙).
    [Serializable]
    public class EmberBrandAura : IHeroAura, IDebuffAxisAura, IPenaltyScalable
    {
        public bool IsDebuffAxis => true;

        private readonly float _ratioPerSec;
        private float _acc;

        public EmberBrandAura(float ratioPerSec)
        {
            _ratioPerSec = ratioPerSec;
        }

        public float RatioPerSec => _ratioPerSec;

        public IHeroAura WithPenaltyScale(float penaltyMul) => new EmberBrandAura(_ratioPerSec * penaltyMul);

        //# 1초 틱 피해 = Max(1, Round(최대HP x ratio)) — 순수 계산(테스트 가능).
        public int DamagePerTick(int maxHp) => Mathf.Max(1, Mathf.RoundToInt(maxHp * _ratioPerSec));

        public void OnAttached(IHealth hero)
        {
            _acc = 0f;
        }

        public void Tick(IHealth hero, float dt)
        {
            if (hero == null)
                return;
            _acc += dt;
            while (_acc >= 1f)
            {
                _acc -= 1f;
                //# 데미지 숫자 색 스탬프 — TakeDamage(OnChanged 동기 발행) 직전.
                StampColor(hero, HitFeedbackPalette.Ember);
                hero.TakeDamage(DamagePerTick(hero.Max));
            }
        }

        public void OnDetached(IHealth hero) { }

        private static void StampColor(IHealth hero, Color c)
        {
            if (hero is Component comp && comp != null)
                comp.GetComponent<LairCharacter>()?.Get<IDamageColorSink>()?.StampDamageColor(c);
        }
    }
}
