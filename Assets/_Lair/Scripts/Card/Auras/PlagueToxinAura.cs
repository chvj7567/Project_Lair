using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 역병귀 독 (monster-growth-currencies §15.1) — 영웅 몸에 붙는 중첩형 지속 피해. 모든 중첩이 한 타이머(러너 슬롯 Remain)를 공유한다.
    //# 재부착은 러너가 Merge 로 합친다: 중첩 + addStacks(상한 maxStack), 1중첩 피해 Max, 지속은 갱신(연장 누적 아님).
    [Serializable]
    public class PlagueToxinAura : IHeroAura, IStatusVisual, IDebuffAxisAura
    {
        public bool IsDebuffAxis => true;

        //# 상태 아이콘 = "역병의 손길" 카드 아이콘 재사용 (신규 스프라이트 없음).
        public ECardId IconCardId => ECardId.PlagueSlowBoost;

        private float _perStackDamage;
        private int _stacks;
        private int _maxStack;
        private float _tickAccumulator;
        [NonSerialized] private IDamageColorSink _colorSink;

        public int Stacks => _stacks;
        public int MaxStack => _maxStack;
        public float PerStackDamage => _perStackDamage;

        //# addStacks = 이번 부착이 더하는 중첩(적중 1, 전염 1~3). maxStack = 이 부착 시점의 상한(기본 3, Debuff Tier1 이면 5).
        public PlagueToxinAura(float perStackDamage, int addStacks, int maxStack)
        {
            _maxStack = Math.Max(1, maxStack);
            _perStackDamage = Math.Max(0f, perStackDamage);
            _stacks = Math.Max(1, Math.Min(_maxStack, addStacks));
        }

        //# 같은 타입 재부착 — 상한은 두 값 중 큰 쪽, 피해는 Max(현재, 새 값), 중첩은 상한까지 더한다(E-14 상한이면 유지).
        public void Merge(PlagueToxinAura incoming)
        {
            if (incoming == null)
                return;
            _maxStack = Math.Max(_maxStack, incoming._maxStack);
            _perStackDamage = Math.Max(_perStackDamage, incoming._perStackDamage);
            _stacks = Math.Min(_maxStack, _stacks + incoming._stacks);
        }

        public void OnAttached(IHealth hero)
        {
            _tickAccumulator = 0f;
            _colorSink = hero is Component comp && comp != null ? comp.GetComponent<LairCharacter>()?.Get<IDamageColorSink>() : null;
        }

        //# 1초마다 Max(1, Round(중첩 x 1중첩 피해)) — PoisonAura 의 1초 누적 틱 패턴.
        public void Tick(IHealth hero, float dt)
        {
            if (hero == null)
                return;
            int ticks = AccumulateTicks(dt);
            for (int i = 0; i < ticks; ++i)
            {
                _colorSink?.StampDamageColor(HitFeedbackPalette.Poison);
                hero.TakeDamage(DamagePerTick());
            }
        }

        public int DamagePerTick() => Math.Max(1, (int)Math.Round(_stacks * _perStackDamage, MidpointRounding.AwayFromZero));

        //# 1초 누적 코어 (순수 로직 — 테스트 가능). 이번 호출의 틱 수 반환.
        public int AccumulateTicks(float dt)
        {
            _tickAccumulator += dt;
            int ticks = 0;
            while (_tickAccumulator >= 1f)
            {
                _tickAccumulator -= 1f;
                ++ticks;
            }
            return ticks;
        }

        public void OnDetached(IHealth hero)
        {
            _colorSink = null;
            _stacks = 0;
        }
    }
}
