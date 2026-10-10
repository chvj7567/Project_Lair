using System;

namespace Lair.Battle
{
    //# 스테이지 진행/스탯 스케일 순수 헬퍼 (chapter-system §2.1). Unity 비의존 → EditMode 단위 검증 가능.
    //# 진행 저장은 선형 스테이지 번호 = (chapterId-1) x 5 + stageIndex. (챕터, 인덱스) 는 변환 함수로 도출.
    public static class StageProgress
    {
        //# 챕터당 스테이지 수 — 5 고정(chapter-system D-7). 보스 = stageIndex 5.
        public const int StagesPerChapter = 5;

        //# 챕터 수 기본값 — Chapter.json 로드 전/테스트용(실제 출시 값은 Chapter.json row 수, 현재 3). 로드 후 ConfigureChapterCount 로 덮는다.
        public const int DefaultChapterCount = 2;

        private static int _chapterCount = DefaultChapterCount;

        //# 출시 챕터 수 x 5 (Chapter.json 3행 로드 후 15). Chapter.json row 수 기반 런타임 값.
        public static int MaxStage => _chapterCount * StagesPerChapter;

        public static int ChapterCount => _chapterCount;

        //# Chapter.json 로드 직후 1회 호출 — 챕터 수가 1 미만이면 무시(MaxStage 0 방지).
        public static void ConfigureChapterCount(int chapterCount)
        {
            if (chapterCount < 1)
                return;
            _chapterCount = chapterCount;
        }

        //# (chapterId, stageIndex) → 선형 번호. 예: (2, 2) → 7.
        public static int ToLinear(int chapterId, int stageIndex)
        {
            return (chapterId - 1) * StagesPerChapter + stageIndex;
        }

        //# 선형 번호 → 챕터(1~). linear < 1 은 1 챕터로 취급.
        public static int ChapterOf(int linear)
        {
            return Math.Max(0, linear - 1) / StagesPerChapter + 1;
        }

        //# 선형 번호 → 챕터 내 인덱스(1~5). linear < 1 은 1 로 취급.
        public static int IndexOf(int linear)
        {
            return Math.Max(0, linear - 1) % StagesPerChapter + 1;
        }

        //# 보스 스테이지 판정 — 챕터 내 마지막(5) 인덱스.
        public static bool IsBossStage(int stageIndex)
        {
            return stageIndex == StagesPerChapter;
        }

        //# 승리 시 클리어 최고 스테이지 갱신 = Max(기존, 방금 클리어). MaxStage 종점(초과 없음, spec §6.1).
        public static int ResolveClearedStage(int cleared, int justClearedStage)
        {
            return Math.Min(MaxStage, Math.Max(cleared, justClearedStage));
        }

        //# 해금 판정 단일 소유(기획서 §4.6) — stage 는 clearedStage+1 까지 해금, 그 이상 잠금. 챕터 경계도 그대로 통과.
        public static bool IsUnlocked(int stage, int clearedStage)
        {
            return stage <= clearedStage + 1;
        }

        //# 챕터 해금 = 그 챕터 1스테이지 해금 (chapter-system §2.1).
        public static bool IsChapterUnlocked(int chapterId, int clearedStage)
        {
            return IsUnlocked(ToLinear(chapterId, 1), clearedStage);
        }

        //# 진입 시 초기 위치 = 최대 플레이 가능 스테이지 = clearedStage+1 (1 하한·MaxStage 상한).
        public static int MaxPlayableStage(int clearedStage)
        {
            return Math.Max(1, Math.Min(MaxStage, clearedStage + 1));
        }

        //# baseline × 배수를 round-half-up 정수화 (기획서 §2.1 — 예: Power 50×1.35=67.5 → 68). 최소 1.
        public static int ScaleStat(int baseValue, float multiplier)
        {
            return Math.Max(1, (int)Math.Floor(baseValue * multiplier + 0.5f));
        }

        //# 영웅 스탯 = 앵커 x 직업 배율 x 스테이지 배수를 1회만 round-half-up (chapter-system §2.1).
        //# float 곱의 표현 오차(0.7f*1.3f*50 = 45.4999..)로 .5 경계가 흔들리지 않게 decimal(7자리 유효숫자 정규화)로 계산한다.
        public static int ScaleHeroStat(int anchor, float archetypeScale, float stageMultiplier)
        {
            decimal exact = (decimal)anchor * (decimal)archetypeScale * (decimal)stageMultiplier;
            return Math.Max(1, (int)Math.Floor(exact + 0.5m));
        }
    }
}
