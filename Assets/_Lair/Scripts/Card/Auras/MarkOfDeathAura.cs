using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 죽음의 표식 (Dps A).
    //# 영웅에 부착되어 _dmgTakenMul 배율로 받는 데미지 ↑. Detach 시 자연 복원.
    //# Health.DamageTakenScale 필드를 곱연산으로 누적 / Detach 시 동일 배율로 나누어 복원.
    //# 기존 BleedAura·HeroAttackDownAura 가 영웅 컴포넌트(IMover/IAttacker) 를 받는 패턴과 동일하게
    //# 부착 대상 IHealth 의 구체 타입 Health 가 노출하는 DamageTakenScale 을 직접 곱연산.
    [Serializable]
    public class MarkOfDeathAura : IHeroAura, IStatusVisual
    {
        //# 비주얼 키 — Bleed 아이콘 재사용 (전용 키 신설은 MVP 범위 외).
        public EVisual VisualKey => EVisual.BleedStatus;
        public Vector3 Offset => new Vector3(-0.4f, 0.05f, 0f);

        private readonly float _dmgTakenMul;
        private Health _heroHealth;
        private bool _applied;

        public MarkOfDeathAura(float dmgTakenMul = 1.5f)
        {
            _dmgTakenMul = dmgTakenMul;
        }

        public void OnAttached(IHealth hero)
        {
            //# Hero 의 구체 Health 컴포넌트에 곱연산. 다른 IHealth 구현체에는 적용 안 됨(테스트는 stub 경로로 분기).
            Health h = hero as Health;
            if (h == null) return;
            if (_applied) return;
            h.DamageTakenScale *= _dmgTakenMul;
            _heroHealth = h;
            _applied = true;
        }

        public void Tick(IHealth hero, float dt) { }

        //# Mark 부착 종료 시 곱연산 복원 — Mark 가 지속시간만큼만 작용한다는 디자인 보장 (§10.4).
        public void OnDetached(IHealth hero)
        {
            if (_applied == false || _heroHealth == null) return;
            if (_dmgTakenMul > 0f)
                _heroHealth.DamageTakenScale /= _dmgTakenMul;
            _applied = false;
            _heroHealth = null;
        }
    }
}
