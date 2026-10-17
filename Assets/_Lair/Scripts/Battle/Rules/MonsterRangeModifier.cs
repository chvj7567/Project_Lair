using System;

namespace Lair.Battle
{
    //# 몬스터 공격 사거리 규칙 상태 (DenseFog) — 비활성이면 입력을 그대로 돌려줘 기존 동작과 동일.
    //# 활성이면 range x mul, 단 하한 max(minRange, 충돌 거리 + 여유)를 지키되 원래 사거리가 하한보다 작으면 늘리지 않는다(E-7).
    public class MonsterRangeModifier
    {
        //# 충돌 거리 위 여유 — chapter-system E-7 "충돌 거리 + 0.1" (명세 고정값).
        public const float ClearanceMargin = 0.1f;

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

        //# collisionDistance = 영웅 + 해당 몬스터의 콜라이더 반경 합(중심 거리 기준 닿는 거리). 0 이면 minRange 만 하한.
        public float Apply(float range, float collisionDistance = 0f)
        {
            if (IsActive == false)
                return range;
            float floor = Math.Max(_minRange, collisionDistance + ClearanceMargin);
            return Compute(range, _mul, floor);
        }

        //# 사거리 보정 공식 — range x mul, 단 하한 floor 아래로는 줄이지 않고 원래 사거리가 하한보다 작으면 늘리지 않는다. 안개(규칙)·연막(스킬) 공용.
        public static float Compute(float range, float mul, float floor)
        {
            return Lair.Character.RangeScaleMath.Compute(range, mul, floor);
        }
    }
}
