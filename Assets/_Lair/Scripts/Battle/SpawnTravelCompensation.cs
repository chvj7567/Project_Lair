using UnityEngine;

namespace Lair.Battle
{
    //# 스포너 이동 시간 보존(정적 순수) — scene-2d-conversion §3.5.4 · §7.8.
    //# 스포너를 영웅 쪽으로 당겨 줄어든 이동 시간만큼 첫 스폰만 늦춘다.
    public static class SpawnTravelCompensation
    {
        //# 보정 기준이 되는 이전 스포너 반지름(전 스포너 공통)
        public const float LegacyRadius = 20f;
        //# 지연 계수 c — qa 게이트에서만 조정하는 코드 상수
        public const float DelayScale = 1f;

        //# max(0, (max(0, 옛거리 − 사거리) − max(0, 새거리 − 사거리)) / 이동속도) × 계수
        public static float FirstSpawnDelay(float legacyRadius, float distance, float range, float moveSpeed, float scale)
        {
            if (moveSpeed <= 0f)
                return 0f;
            float legacyTravel = Mathf.Max(0f, legacyRadius - range);
            float newTravel = Mathf.Max(0f, distance - range);
            return Mathf.Max(0f, (legacyTravel - newTravel) / moveSpeed) * scale;
        }
    }
}
