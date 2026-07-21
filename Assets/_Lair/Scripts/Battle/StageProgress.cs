using System;

namespace Lair.Battle
{
    //# 스테이지 진행/스탯 스케일 순수 헬퍼 (hero-stage-variant plan Task 6). Unity 비의존 → EditMode 단위 검증 가능.
    public static class StageProgress
    {
        public const int MaxStage = 5;

        //# 승리 시 클리어 최고 스테이지 갱신 = Max(기존, 방금 클리어). 5 종점(초과 없음, spec §6.1).
        public static int ResolveClearedStage(int cleared, int justClearedStage)
        {
            return Math.Min(MaxStage, Math.Max(cleared, justClearedStage));
        }

        //# 해금 판정 단일 소유(기획서 §4.6) — stage 는 clearedStage+1 까지 해금, 그 이상 잠금. 캐러셀 VM/컨트롤러가 호출.
        public static bool IsUnlocked(int stage, int clearedStage)
        {
            return stage <= clearedStage + 1;
        }

        //# baseline × 배수를 round-half-up 정수화 (기획서 §2.1 — 예: Power 50×1.35=67.5 → 68). 최소 1.
        public static int ScaleStat(int baseValue, float multiplier)
        {
            return Math.Max(1, (int)Math.Floor(baseValue * multiplier + 0.5f));
        }
    }
}
