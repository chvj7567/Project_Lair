using System;
using Lair.Character;
using Lair.Data;

namespace Lair.Card
{
    //# 저주술사 특성 "저주 누적" (monster-growth-currencies §3.4) — 적중마다 영웅이 받는 피해 +perStack/중첩(최대 maxStack), 마지막 적중 후 지속 갱신.
    //# 받는 피해 배율은 영웅 DamageTakenScale 에 곱/나눗셈으로 합성(MarkOfDeath·수호 결계 선례). 성기사 저항은 가산분에 적용(러너가 ScalePenalty).
    [Serializable]
    public class CurseStackAura : IHeroAura, IStatusVisual, IDebuffAxisAura, IStackedStatus
    {
        public bool IsDebuffAxis => true;

        //# 상태 아이콘 = 저주술사 카드 아이콘 재사용 (신규 스프라이트 없음).
        public ECardId IconCardId => ECardId.HexRangeBoost;

        private float _perStack;
        private readonly int _maxStack;
        private int _stacks;
        private float _appliedFactor = 1f;
        [NonSerialized] private Health _health;

        public int Stacks => _stacks;
        public int StackCount => _stacks;
        public float Factor => 1f + _perStack * _stacks;

        public CurseStackAura(float perStack, int maxStack)
        {
            _perStack = Math.Max(0f, perStack);
            _maxStack = Math.Max(1, maxStack);
            _stacks = 1;
        }

        //# 신앙의 가호 — 가산분 x penaltyMul (CursedBanner 의 IPenaltyScalable 선례). 부착 전 1회.
        public void ScalePenalty(float penaltyMul)
        {
            _perStack *= penaltyMul;
        }

        //# 같은 타입 재적중 — 중첩 +1 (상한 유지), 가산분은 큰 값.
        public void Merge(CurseStackAura incoming)
        {
            if (incoming == null)
                return;
            _perStack = Math.Max(_perStack, incoming._perStack);
            _stacks = Math.Min(_maxStack, _stacks + 1);
            Reapply();
        }

        public void OnAttached(IHealth hero)
        {
            _health = hero as Health;
            _appliedFactor = 1f;
            Reapply();
        }

        public void Tick(IHealth hero, float dt) { }

        public void OnDetached(IHealth hero)
        {
            if (_health != null && _appliedFactor > 0f)
            {
                _health.DamageTakenScale /= _appliedFactor;
            }
            _appliedFactor = 1f;
            _health = null;
        }

        private void Reapply()
        {
            if (_health == null)
                return;
            float next = Factor;
            _health.DamageTakenScale = _health.DamageTakenScale / _appliedFactor * next;
            _appliedFactor = next;
        }
    }
}
