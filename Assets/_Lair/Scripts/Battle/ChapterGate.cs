namespace Lair.Battle
{
    //# 출격 가능 챕터 게이트 — 단일 소유. 월드맵·마을 캐러셀·출격이 이 값으로 챕터를 막는다. 슬라이스 (d3) 에서 Ch2(궁수 프리팹·AI·아트 완성)까지 열었다.
    //# 새 챕터는 에셋·데이터가 갖춰진 뒤 이 값만 올리면 한 번에 풀린다. Unity 비의존.
    public static class ChapterGate
    {
        //# 현재 출격 가능한 챕터 수 — Ch1 지하 묘지 + Ch2 어둠의 숲.
        public const int PlayableChapterCount = 2;

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
