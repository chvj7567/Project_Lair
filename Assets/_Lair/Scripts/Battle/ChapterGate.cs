namespace Lair.Battle
{
    //# 출격 가능 챕터 게이트 — 단일 소유. Ch2(궁수 프리팹·AI·아트)가 준비되기 전까지 월드맵·캐러셀·출격이 이 값으로 Ch2 를 막는다.
    //# 슬라이스 (d) 에서 PlayableChapterCount 를 챕터 수(2)로 올리면 한 번에 풀린다. Unity 비의존.
    public static class ChapterGate
    {
        //# 현재 출격 가능한 챕터 수. (d) 에서 2 로 상향.
        public const int PlayableChapterCount = 1;

        //# 출격 가능한 선형 스테이지 상한 = 가능 챕터 수 x 5.
        public static int PlayableStageCount => PlayableChapterCount * StageProgress.StagesPerChapter;

        public static bool IsChapterPlayable(int chapterId)
        {
            return chapterId >= 1 && chapterId <= PlayableChapterCount;
        }

        public static bool IsStagePlayable(int linearStage)
        {
            return linearStage >= 1 && linearStage <= PlayableStageCount;
        }
    }
}
