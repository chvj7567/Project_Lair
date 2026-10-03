using System;

namespace Lair.Battle
{
    //# 몬스터 공격 사거리 규칙 상태 (DenseFog) — 비활성이면 입력을 그대로 돌려줘 기존 동작과 동일.
    //# 활성이면 range x mul, 단 하한 minRange 를 지키되 원래 사거리가 하한보다 작으면 늘리지 않는다.
    public class MonsterRangeModifier
    {
        private float _mul = 1f;
        private float _minRange;

        public bool IsActive { get; private set; }

        public void Set(float mul, float minRange)
        {
            _mul = mul;
            _minRange = minRange;
            IsActive = true;
        }

        public void Clear()
        {
            _mul = 1f;
            _minRange = 0f;
            IsActive = false;
        }

        public float Apply(float range)
        {
            if (IsActive == false)
                return range;
            return Math.Max(Math.Min(range, _minRange), range * _mul);
        }
    }
}
