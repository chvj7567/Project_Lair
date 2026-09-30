using Lair.Battle;

namespace Lair.Net
{
    //# 스테이지별 리더보드(stageLeaderboard/{stage}_{uid}) 규약 — Firebase 타입 비의존 순수 헬퍼라 게이트 밖에 둔다.
    //# 문서 필드: uid / stage / displayName / clearTimeMs / hero. 보안 규칙·복합 인덱스(stage, clearTimeMs)는 Firebase 콘솔 소관.
    public static class StageLeaderboard
    {
        public const string CollectionName = "stageLeaderboard";
        public const int MaxStage = StageProgress.MaxStage;

        //# 스테이지 범위(1~5) 검증 — 범위 밖이면 서버 왕복 없이 실패/빈 목록.
        public static bool IsValidStage(int stage)
        {
            return stage >= 1 && stage <= StageProgress.MaxStage;
        }

        //# 문서 ID = "{stage}_{uid}" — 한 유저의 한 스테이지 최단 기록 1건.
        public static string DocId(int stage, string uid)
        {
            return $"{stage}_{uid}";
        }

        //# 새 기록을 쓸지 판정 — 기존 기록이 없거나(<=0) 새 기록이 더 빠를 때만. 동률은 쓰지 않는다.
        public static bool ShouldReplace(long existingClearTimeMs, int newClearTimeMs)
        {
            if (newClearTimeMs <= 0)
                return false;
            return existingClearTimeMs <= 0 || newClearTimeMs < existingClearTimeMs;
        }
    }
}
