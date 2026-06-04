using UnityEngine;

namespace Lair.Character
{
    //# 영웅 스킬 데미지 영역 멤버십 — XZ 평면 순수 기하. Unity 월드 비의존(테스트 가능).
    public static class SkillGeometry
    {
        //# p 가 center 기준 XZ 링 밴드 [inner, outer] 안인가. inner=0 이면 디스크.
        public static bool InRing(Vector3 p, Vector3 center, float inner, float outer)
        {
            float dx = p.x - center.x;
            float dz = p.z - center.z;
            float sq = dx * dx + dz * dz;
            return sq >= inner * inner && sq <= outer * outer;
        }

        //# p 가 origin 에서 dir(정규화 가정) 기준 radial 부채꼴 안인가 (XZ).
        //# radial 판정 — 반경거리 <= length AND dir 과의 각도 <= halfAngleDeg. (InLine 의 축투영 미러 금지 — 비주얼 팬 mesh 와 정합.)
        public static bool InCone(Vector3 p, Vector3 origin, Vector3 dir, float length, float halfAngleDeg)
        {
            Vector3 d = dir;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f)
                return false;
            d.Normalize();

            Vector3 rel = p - origin;
            rel.y = 0f;
            float distSq = rel.sqrMagnitude;
            if (distSq > length * length)
                return false;
            if (distSq < 0.0001f)
                return true;   //# 원점과 동일 위치 — 부채꼴 꼭짓점, 각도 무관 포함.

            float cos = Vector3.Dot(rel.normalized, d);
            float cosHalf = Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
            return cos >= cosHalf;
        }

        //# p 가 center 기준 XZ 거리 radius 안인가. 블레이드 구·몬스터 모두 y≈0 수평 공전.
        public static bool InSphere(Vector3 p, Vector3 center, float radius)
        {
            float dx = p.x - center.x;
            float dz = p.z - center.z;
            return dx * dx + dz * dz <= radius * radius;
        }
    }
}
