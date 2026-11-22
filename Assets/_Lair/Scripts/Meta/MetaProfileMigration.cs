using System;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;

namespace Lair.Meta
{
    //# MetaProfile v4 → v5 → v6 → v7 → v8 → v9 → v10 → v11 → v12 → v13 마이그레이션 (chapter-system §2.2, chapter-3-hero §7.3) — 스키마 변경 없이 ClearedStage/SelectedStage 범위 의미만 0~5 → 0~MaxStage(v6 = 챕터 3 추가로 15).
    //# 멱등 — 로컬 로드 경로와 클라우드 복원(CopyFrom) 직후 동일 함수로 재적용한다. Unity 비의존.
    public static class MetaProfileMigration
    {
        public const int CurrentVersion = 14;

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
            //# 유물(v8) 정리는 챕터 데이터에 의존하지 않으므로 이 조기 반환보다 앞서 항상 수행한다(멱등).
            NormalizeRelics(profile);
            //# 편성(v9)도 챕터 데이터·편성 JSON 에 의존하지 않는 모양 정리라 같은 이유로 조기 반환보다 앞선다(dungeon-formation §4.1).
            NormalizeFormation(profile);
            MonsterEnhanceTierRules.NormalizeProfile(profile);
            //# 탑(v10)도 챕터 데이터에 의존하지 않는 행 정리라 같은 이유로 조기 반환보다 앞선다(infinite-tower §5.1). 시즌 정리는 여기서 하지 않는다 — TowerConfig 가 로드 전일 수 있다.
            NormalizeTower(profile);
            //# 데일리(v11)도 데이터 비의존 행 정리라 같은 이유로 조기 반환보다 앞선다(daily-challenge §5.1). attemptsPerDay 클램프는 하지 않는다(설정 로드 전일 수 있음).
            NormalizeDaily(profile);
            //# 육성 재화(v12)도 데이터 비의존 행 정리라 같은 이유로 조기 반환보다 앞선다(monster-growth-currencies §7.3). 소급 지급은 하지 않는다(정산 함수 소관).
            NormalizeCurrencies(profile);
            //# 몬스터 등급(v13)도 데이터 비의존 행 정리라 같은 이유로 조기 반환보다 앞선다(monster-grades §8). 구버전은 빈 목록 = 모든 종족 1등급.
            NormalizeRaceGrades(profile);
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

        //# Relics: null→빈 리스트, null/빈 id/Stack<1 행 제거, 같은 RelicId 중복은 Stack 최댓값 한 행. RelicMissStreak 음수→0.
        //# 유물 id 존재 여부·maxStack 클램프는 하지 않는다 — 읽는 지점이 담당(relics §3.6, 데이터 로드 순서 방어).
        public static void NormalizeRelics(MetaProfile profile)
        {
            if (profile.Relics == null)
            {
                profile.Relics = new List<RelicEntry>();
            }
            profile.Relics.RemoveAll(entry => entry == null || string.IsNullOrEmpty(entry.RelicId) || entry.Stack < 1);

            List<RelicEntry> merged = new List<RelicEntry>();
            foreach (RelicEntry entry in profile.Relics)
            {
                RelicEntry existing = merged.Find(e => e.RelicId == entry.RelicId);
                if (existing == null)
                {
                    merged.Add(entry);
                    continue;
                }
                existing.Stack = Math.Max(existing.Stack, entry.Stack);
            }
            profile.Relics = merged;

            if (profile.RelicMissStreak < 0)
            {
                profile.RelicMissStreak = 0;
            }
        }

        //# v13 정상 6행은 수동 편성으로 이행한다. 소유 검사와 중복 복구는 데이터 로드 후에 한다.
        public static void NormalizeFormation(MetaProfile profile)
        {
            if (profile.Version < 14)
            {
                profile.HasCustomFormation = FormationSave.FromSaved(profile.Formation) != null;
            }
            if (profile.HasCustomFormation == false || profile.Formation == null)
            {
                profile.Formation = new List<FormationSlotEntry>();
            }
        }

        //# TowerFloorClears: null→빈 리스트, null/Floor<1/ClearTime<=0 행 제거, 같은 Floor 중복은 ClearTime 최솟값 한 행. TowerSeason 음수→0.
        public static void NormalizeTower(MetaProfile profile)
        {
            if (profile.TowerFloorClears == null)
            {
                profile.TowerFloorClears = new List<TowerFloorClearEntry>();
            }
            profile.TowerFloorClears.RemoveAll(entry => entry == null || entry.Floor < 1 || entry.ClearTime <= 0f);

            List<TowerFloorClearEntry> merged = new List<TowerFloorClearEntry>();
            foreach (TowerFloorClearEntry entry in profile.TowerFloorClears)
            {
                TowerFloorClearEntry existing = merged.Find(e => e.Floor == entry.Floor);
                if (existing == null)
                {
                    merged.Add(entry);
                    continue;
                }
                existing.ClearTime = Math.Min(existing.ClearTime, entry.ClearTime);
                //# 한쪽이라도 지급됐으면 지급된 층 — 중복 행 병합이 재지급을 만들지 않게.
                existing.CrystalPaid = existing.CrystalPaid || entry.CrystalPaid;
            }
            profile.TowerFloorClears = merged;

            if (profile.TowerSeason < 0)
            {
                profile.TowerSeason = 0;
            }
        }

        //# DailyDays: null/Day<0/AttemptsUsed<1 행 제거, 같은 Day 는 AttemptsUsed 최댓값. DailyResults: null/Day<0/점수 범위 밖 제거, 같은 Day 는 BestScore 최솟값. DailyStreak 음수→0.
        public static void NormalizeDaily(MetaProfile profile)
        {
            if (profile.DailyDays == null)
            {
                profile.DailyDays = new List<DailyDayEntry>();
            }
            profile.DailyDays.RemoveAll(entry => entry == null || entry.Day < 0 || entry.AttemptsUsed < 1);
            List<DailyDayEntry> days = new List<DailyDayEntry>();
            foreach (DailyDayEntry entry in profile.DailyDays)
            {
                DailyDayEntry existing = days.Find(e => e.Day == entry.Day);
                if (existing == null)
                {
                    days.Add(entry);
                    continue;
                }
                existing.AttemptsUsed = Math.Max(existing.AttemptsUsed, entry.AttemptsUsed);
            }
            profile.DailyDays = days;

            if (profile.DailyResults == null)
            {
                profile.DailyResults = new List<DailyResultEntry>();
            }
            profile.DailyResults.RemoveAll(entry => entry == null || entry.Day < 0 || DailyRules.IsValidScore(entry.BestScore) == false);
            List<DailyResultEntry> results = new List<DailyResultEntry>();
            foreach (DailyResultEntry entry in profile.DailyResults)
            {
                DailyResultEntry existing = results.Find(e => e.Day == entry.Day);
                if (existing == null)
                {
                    results.Add(entry);
                    continue;
                }
                existing.BestScore = Math.Min(existing.BestScore, entry.BestScore);
            }
            profile.DailyResults = results;

            if (profile.DailyStreak < 0)
            {
                profile.DailyStreak = 0;
            }
        }

        //# Currencies (§7.3-4): null→빈 리스트, null 행/빈 id/Soul 행 제거, Amount<0→0, TotalEarned<Amount→Amount, TotalEarned<1 행 제거, 같은 id 중복은 각 최댓값 한 행.
        //# ECurrency 에 없는 id 는 지우지 않는다 — 읽는 지점이 무시한다(relics §3.6 선례).
        //# RaceGrades: null→빈 리스트, ERace 값명이 아니거나 등급 2~5 밖인 행 제거, 같은 종족 중복은 최고 등급 한 행.
        //# 종족별 데이터 최고 등급 클램프는 하지 않는다 — Race_Monster 로드 전일 수 있다(읽는 지점이 보유 등급 이하 몬스터만 고른다).
        public static void NormalizeRaceGrades(MetaProfile profile)
        {
            List<RaceGradeEntry> merged = new List<RaceGradeEntry>();
            if (profile.RaceGrades != null)
            {
                foreach (RaceGradeEntry entry in profile.RaceGrades)
                {
                    if (entry == null || FormationDatabase.TryParseEnum(entry.Race, out ERace _) == false)
                        continue;
                    if (entry.Grade <= MonsterGrades.MinGrade || entry.Grade > MonsterGrades.MaxGrade)
                        continue;
                    RaceGradeEntry existing = merged.Find(e => e.Race == entry.Race);
                    if (existing == null)
                    {
                        merged.Add(new RaceGradeEntry { Race = entry.Race, Grade = entry.Grade });
                        continue;
                    }
                    existing.Grade = Math.Max(existing.Grade, entry.Grade);
                }
            }
            profile.RaceGrades = merged;
        }

        public static void NormalizeCurrencies(MetaProfile profile)
        {
            if (profile.Currencies == null)
            {
                profile.Currencies = new List<CurrencyEntry>();
            }
            profile.Currencies.RemoveAll(entry => entry == null || string.IsNullOrEmpty(entry.CurrencyId) || entry.CurrencyId == ECurrency.Soul.ToString());
            foreach (CurrencyEntry entry in profile.Currencies)
            {
                entry.Amount = Math.Max(0, entry.Amount);
                entry.TotalEarned = Math.Max(entry.TotalEarned, entry.Amount);
            }
            profile.Currencies.RemoveAll(entry => entry.TotalEarned < 1);

            List<CurrencyEntry> merged = new List<CurrencyEntry>();
            foreach (CurrencyEntry entry in profile.Currencies)
            {
                CurrencyEntry existing = merged.Find(e => e.CurrencyId == entry.CurrencyId);
                if (existing == null)
                {
                    merged.Add(entry);
                    continue;
                }
                existing.Amount = Math.Max(existing.Amount, entry.Amount);
                existing.TotalEarned = Math.Max(existing.TotalEarned, entry.TotalEarned);
            }
            profile.Currencies = merged;
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
                existing.AwakenPaid = Math.Max(existing.AwakenPaid, entry.AwakenPaid);
            }
            //# 각성석 지급 기록 불변식 0 <= AwakenPaid <= MaxDifficulty (§7.3-5).
            foreach (DifficultyClearEntry entry in merged)
            {
                entry.AwakenPaid = Math.Max(0, Math.Min(entry.MaxDifficulty, entry.AwakenPaid));
            }
            profile.DifficultyClears = merged;

            if (profile.SelectedDifficulty < 0)
            {
                profile.SelectedDifficulty = 0;
            }
        }
    }
}
