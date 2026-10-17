using UnityEngine;

namespace Lair.Stage
{
    //# 도트 무대 좌표 매핑(정적 순수) — 기획서 scene-2d-conversion §1.2 · §7.1.
    //# 도트 좌표는 시안과 같이 좌상단 원점·아래 +y, 값은 픽셀 모서리 기준.
    public static class DotStageMapping
    {
        public const float Ppu = 48f;

        //# 배틀 — 피치 50°, 주시점 (0,0,−1.60904) ↔ 배경 도트 (614,352)
        public static readonly Vector2 BattleScreenCenterDot = new Vector2(614f, 352f);
        public const float BattleLookZ = -1.60904f;
        private static readonly float BattleSin = Ppu * Mathf.Sin(50f * Mathf.Deg2Rad);
        private static readonly float BattleCos = Ppu * Mathf.Cos(50f * Mathf.Deg2Rad);

        //# 마을 — 피치 16°, HeroAnchor (0,0,0) ↔ 홀로그램 발밑 도트 (240,221), 화면 중앙 (240,135)
        public static readonly Vector2 VillageScreenCenterDot = new Vector2(240f, 135f);
        private static readonly Vector2 VillageAnchorDot = new Vector2(240f, 221f);
        private static readonly float VillageSin = Ppu * Mathf.Sin(16f * Mathf.Deg2Rad);
        private static readonly float VillageCos = Ppu * Mathf.Cos(16f * Mathf.Deg2Rad);

        //# 로딩 — 정면, 모든 요소가 z=0 평면
        public static readonly Vector2 LoadingScreenCenterDot = new Vector2(240f, 135f);

        public static Vector2 BattleGroundToDot(Vector3 world)
        {
            return new Vector2(
                BattleScreenCenterDot.x + Ppu * world.x,
                BattleScreenCenterDot.y - BattleSin * (world.z - BattleLookZ) - BattleCos * world.y);
        }

        public static Vector3 BattleDotToGround(Vector2 dot)
        {
            return new Vector3(
                (dot.x - BattleScreenCenterDot.x) / Ppu,
                0f,
                (BattleScreenCenterDot.y - dot.y) / BattleSin + BattleLookZ);
        }

        public static Vector2 VillageGroundToDot(Vector3 world)
        {
            return new Vector2(
                VillageAnchorDot.x + Ppu * world.x,
                VillageAnchorDot.y - VillageSin * world.z - VillageCos * world.y);
        }

        public static Vector3 VillageDotToGround(Vector2 dot)
        {
            return new Vector3(
                (dot.x - VillageAnchorDot.x) / Ppu,
                0f,
                (VillageAnchorDot.y - dot.y) / VillageSin);
        }

        //# 배경 도트 → @DotStage 로컬 (화면 위 = +y)
        public static Vector2 StageLocalFromDot(Vector2 dot, Vector2 screenCenterDot)
        {
            return new Vector2((dot.x - screenCenterDot.x) / Ppu, (screenCenterDot.y - dot.y) / Ppu);
        }
    }
}
