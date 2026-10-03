using System;

namespace Lair.Battle
{
    //# 스테이지 표기 순수 헬퍼 — 선형 번호 → "{챕터}-{인덱스}", 위협도 별, 잠금 안내 문구 (chapter-system §6.4). Unity 비의존.
    public static class StageLabels
    {
        //# Strings_Ko id — "스테이지 {0}-{1} 클리어 필요" / "{0}장 보스 클리어 필요".
        public const int HintPreviousStageStringId = 1313;
        public const int HintChapterBossStringId = 1314;

        //# 예: 7 → "2-2".
        public static string Number(int linearStage)
        {
            return $"{StageProgress.ChapterOf(linearStage)}-{StageProgress.IndexOf(linearStage)}";
        }

        //# 챕터 내 인덱스 기준 별 — ★ x 인덱스 + ☆ x (5-인덱스).
        public static string Stars(int stageIndex)
        {
            int filled = Math.Max(0, Math.Min(StageProgress.StagesPerChapter, stageIndex));
            return new string('★', filled) + new string('☆', StageProgress.StagesPerChapter - filled);
        }

        //# 잠긴 스테이지의 선행 조건 안내 — 인덱스 > 1 이면 직전 스테이지, 1 이면 직전 챕터 보스. text 가 null 이면 빈 문자열.
        public static string UnlockHint(int linearStage, Func<int, string> text)
        {
            if (text == null)
                return string.Empty;
            int chapter = StageProgress.ChapterOf(linearStage);
            int index = StageProgress.IndexOf(linearStage);
            if (index > 1)
                return string.Format(text(HintPreviousStageStringId), chapter, index - 1);
            return string.Format(text(HintChapterBossStringId), chapter - 1);
        }
    }
}
