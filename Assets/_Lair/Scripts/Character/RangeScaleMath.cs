namespace Lair.Character
{
    //# 사거리 보정 순수 공식 — 안개(규칙, Battle.MonsterRangeModifier)와 연막(스킬, MeleeAttacker)이 공유한다. Character 쪽에 두어 역방향(Character → Battle) 결합이 없다.
    public static class RangeScaleMath
    {
        //# range x mul, 단 하한 floor 아래로는 줄이지 않고 원래 사거리가 하한보다 작으면 늘리지 않는다.
        public static float Compute(float range, float mul, float floor)
        {
            return System.Math.Max(System.Math.Min(range, floor), range * mul);
        }
    }
}
