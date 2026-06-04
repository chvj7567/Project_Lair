using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 — 죽음의 표식 (Dps A).
    //# 영웅에 부착되어 _dmgTakenMul 배율로 받는 데미지 ↑. Detach 시 자연 복원.
    [Serializable]
    public class MarkOfDeathAura : IHeroAura, IStatusVisual
    {
        public ECardId IconCardId => ECardId.MarkOfDeath;

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
