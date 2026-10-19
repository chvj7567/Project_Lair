using System;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;

namespace Lair.Meta
{
    //# MetaProfile v4 → v5 → v6 → v7 마이그레이션 (chapter-system §2.2, chapter-3-hero §7.3) — 스키마 변경 없이 ClearedStage/SelectedStage 범위 의미만 0~5 → 0~MaxStage(v6 = 챕터 3 추가로 15).
    //# 멱등 — 로컬 로드 경로와 클라우드 복원(CopyFrom) 직후 동일 함수로 재적용한다. Unity 비의존.
    public static class MetaProfileMigration
    {
        public const int CurrentVersion = 7;

        //# 챕터 데이터로 SelectedHero 단계를 수행하는 편의 오버로드 — chapters 가 null 이면 5단계는 건너뛴다.
        public static void Apply(MetaProfile profile, ChapterDatabase chapters)
        {
            Func<int, string> heroIdOf = null;
            if (chapters != null)
            {
                heroIdOf = linear => chapters.GetHeroOfLinear(linear)?.heroId;
            }
            Apply(profile, heroIdOf);
        }

        //# heroIdOfLinearStage: 선형 스테이지 번호 → 그 챕터의 heroId. null 이면(챕터 데이터 미로드) SelectedHero 재설정 단계만 건너뛴다.
        public static void Apply(MetaProfile profile, Func<int, string> heroIdOfLinearStage)
        {
            if (profile == null)
                return;
            //# 챕터 데이터가 아직 로드되지 않았고(MaxStage 가 기본값) 이미 현재 버전이면 진행 범위를 자르지 않는다 —
            //# 챕터 3 이후 Cleared > 10 인 v6 세이브가 로드 전 호출에서 기본 MaxStage(10)로 잘리는 경로 방어. 트레이드오프: DB 로드 실패 시 비정상 v6 값은 자르지 않는다(의도).
            if (heroIdOfLinearStage == null && profile.Version >= CurrentVersion)
                return;

            //# v6 세이브 + 챕터 데이터 미로드 — 기본 MaxStage 로 진행 범위를 자르지 않는다(2·3·4 단계 및 난이도 행 Stage>ClearedStage 정리 생략).
            bool skipRange = heroIdOfLinearStage == null && profile.Version >= 6;

            //# 1) 버전 승격 — 이미 현재 버전 이상이면 유지.
            if (profile.Version < CurrentVersion)
            {
                profile.Version = CurrentVersion;
            }

            if (skipRange == false)
            {
                //# 2) 클리어 진행 범위 보정.
                profile.ClearedStage = Math.Max(0, Math.Min(StageProgress.MaxStage, profile.ClearedStage));

                //# 3) 선택 스테이지는 1 ~ 최대 플레이 가능 스테이지.
                int maxPlayable = StageProgress.MaxPlayableStage(profile.ClearedStage);
                profile.SelectedStage = Math.Max(1, Math.Min(maxPlayable, profile.SelectedStage));

                //# 4) 범위 밖 스테이지 전적 제거.
                if (profile.StageRecords != null)
                {
                    profile.StageRecords.RemoveAll(entry => entry == null || entry.Stage < 1 || entry.Stage > StageProgress.MaxStage);
                }
            }

            //# 5) 난이도(v7, difficulty-select §3.1) — 행 정리/음수 보정만. 상한 클램프는 읽는 순간 ClampDifficulty 가 담당(난이도 데이터 로드 전 호출 방어).
            NormalizeDifficulty(profile, skipRange);

            //# 6) SelectedHero = 선택 스테이지 챕터의 영웅 (표시 전용 필드).
            if (heroIdOfLinearStage == null)
                return;
            string heroId = heroIdOfLinearStage(profile.SelectedStage);
            if (string.IsNullOrEmpty(heroId) == false)
            {
                profile.SelectedHero = heroId;
            }
        }

        //# DifficultyClears: null→빈 리스트, 범위 밖/MaxDifficulty<1/Stage>ClearedStage 행 제거, 같은 Stage 중복은 최댓값 한 행. SelectedDifficulty 음수→0.
        private static void NormalizeDifficulty(MetaProfile profile, bool skipRange)
        {
            if (profile.DifficultyClears == null)
            {
                profile.DifficultyClears = new List<DifficultyClearEntry>();
            }
            profile.DifficultyClears.RemoveAll(entry => entry == null || entry.Stage < 1 || entry.MaxDifficulty < 1
                || (skipRange == false && (entry.Stage > StageProgress.MaxStage || entry.Stage > profile.ClearedStage)));

            List<DifficultyClearEntry> merged = new List<DifficultyClearEntry>();
            foreach (DifficultyClearEntry entry in profile.DifficultyClears)
            {
                DifficultyClearEntry existing = merged.Find(e => e.Stage == entry.Stage);
                if (existing == null)
                {
                    merged.Add(entry);
                    continue;
                }
                existing.MaxDifficulty = Math.Max(existing.MaxDifficulty, entry.MaxDifficulty);
            }
            profile.DifficultyClears = merged;

            if (profile.SelectedDifficulty < 0)
            {
                profile.SelectedDifficulty = 0;
            }
        }
    }
}
