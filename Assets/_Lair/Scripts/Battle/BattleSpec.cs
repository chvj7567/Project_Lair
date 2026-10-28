using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;

namespace Lair.Battle
{
    //# 이번 전투 사양 — 전투 시작에 한 번 해석하고 모든 소비 지점(배경·제목·규칙·외형·영웅 스탯·유물·기록)이 이것만 읽는다 (infinite-tower §8.1).
    //# 챕터 모드는 SelectedStage 에서, 탑 모드는 (시즌 시드, 도전 층) 에서 도출한다. 탑 모드는 SelectedStage/SelectedDifficulty 를 읽지도 쓰지도 않는다.
    public sealed class BattleSpec
    {
        public EBattleMode Mode;
        public int EffectiveDifficulty;     //# 챕터: 클램프된 난이도, 탑: 0 (탑은 난이도 미적용, D-9)
        public int LinearStage;             //# 영웅 외형(variant) 조회용 선형 번호 — 챕터: SelectedStage, 탑: ToLinear(영웅 챕터, 층 위치)
        public int BackdropChapterId;
        public int RuleChapterId;           //# 규칙 조회 키 (ChapterDatabase.GetRules)
        public int RuleStageIndex;
        public int RelicChapterId;          //# 유물 드랍 키 (Stage_Relic)
        public int RelicStageIndex;
        public int RelicDifficulty;         //# 유물 등급 단계 — 챕터: 난이도, 탑: 층 구간 단계
        public HeroArchetypeRow Hero;       //# null = 영웅 데이터 없음
        public StageRow Stage;              //# 챕터 모드 전용 (스탯 배수 출처)
        public HeroArchetype_TowerRow TowerHero;
        public TowerFloor Floor;            //# 탑 모드 전용
        public DailyChallenge Daily;        //# 데일리 모드 전용 — 규칙 2개·오늘의 유물·카드 시드·날짜의 출처 (daily-challenge §10.1)

        public bool IsTower => Mode == EBattleMode.Tower;
        public bool IsDaily => Mode == EBattleMode.Daily;

        //# 이번 판 스테이지 규칙 — 데일리는 프리셋 2개 [상시, 페이즈], 그 외는 (RuleChapterId, RuleStageIndex) 의 규칙. 모든 규칙 소비 지점이 이것만 읽는다.
        public IReadOnlyList<StageRuleData> ResolveRules(ChapterDatabase chapters)
        {
            if (IsDaily)
                return Daily.Rules;
            if (chapters == null)
                return System.Array.Empty<StageRuleData>();
            return chapters.GetRules(RuleChapterId, RuleStageIndex);
        }

        //# HUD 제목·결과창의 챕터 내 단계 번호 (1~5).
        public int StageIndex => StageProgress.IndexOf(LinearStage);

        public bool CanComputeHeroStats => Hero != null && (IsTower ? TowerHero != null && Floor != null : Stage != null);

        //# 영웅 최종 스탯 — 챕터는 Stage.json x 난이도, 탑은 층 배율 (CanComputeHeroStats 가 참일 때만 호출).
        public HeroStats ComputeHeroStats(int anchorHp, int anchorPower)
        {
            if (IsTower)
                return HeroStatCalculator.ComputeTower(anchorHp, anchorPower, Hero, TowerHero, Floor);
            return HeroStatCalculator.Compute(anchorHp, anchorPower, Hero, Stage, DifficultyTable.CurrentRow(EffectiveDifficulty));
        }
    }

    //# 전투 사양 해석 순수 함수 — 모드에 따라 프로필/데이터에서 BattleSpec 을 만든다. Unity 비의존.
    public static class BattleSpecResolver
    {
        //# 챕터 모드는 항상 성공한다(데이터가 없으면 Hero/Stage 가 null — 소비 지점이 기존대로 처리). 탑 모드는 데이터 부재 시 null (E-1).
        //# 데일리 모드는 daily(팝업이 확정한 조합)가 없거나 데이터가 없으면 null — 탑과 같은 진입 중단 경로.
        public static BattleSpec Resolve(EBattleMode mode, MetaProfile profile, ChapterDatabase chapters, TowerDatabase tower, DailyChallenge daily = null)
        {
            if (mode == EBattleMode.Tower)
                return ResolveTower(profile, chapters, tower);
            if (mode == EBattleMode.Daily)
                return ResolveDaily(daily, chapters);
            return ResolveChapter(profile, chapters);
        }

        //# 데일리 (daily-challenge §10.1) — 영웅·배경·외형·스탯 배율은 (영웅 챕터, 기준 스테이지), 난이도는 DailyConfig 고정, 규칙은 프리셋 2개. SelectedStage/SelectedDifficulty 는 읽지도 쓰지도 않는다.
        public static BattleSpec ResolveDaily(DailyChallenge daily, ChapterDatabase chapters)
        {
            if (daily == null || chapters == null)
                return null;
            HeroArchetypeRow hero = chapters.GetHero(daily.HeroId);
            StageRow stage = chapters.GetStage(daily.HeroChapterId, daily.BaseStageIndex);
            if (hero == null || stage == null)
                return null;
            return new BattleSpec
            {
                Mode = EBattleMode.Daily,
                EffectiveDifficulty = daily.Difficulty,
                LinearStage = StageProgress.ToLinear(daily.HeroChapterId, daily.BaseStageIndex),
                BackdropChapterId = daily.HeroChapterId,
                RuleChapterId = daily.HeroChapterId,
                RuleStageIndex = daily.BaseStageIndex,
                RelicChapterId = daily.HeroChapterId,
                RelicStageIndex = daily.BaseStageIndex,
                RelicDifficulty = daily.Difficulty,
                Hero = hero,
                Stage = stage,
                Daily = daily,
            };
        }

        public static BattleSpec ResolveChapter(MetaProfile profile, ChapterDatabase chapters)
        {
            int linear = profile.SelectedStage;
            int difficulty = profile.ClampDifficulty(profile.SelectedDifficulty, linear, DifficultyTable.CurrentMaxIndex);
            int chapterId = StageProgress.ChapterOf(linear);
            int stageIndex = StageProgress.IndexOf(linear);
            return new BattleSpec
            {
                Mode = EBattleMode.Chapter,
                EffectiveDifficulty = difficulty,
                LinearStage = linear,
                BackdropChapterId = chapterId,
                RuleChapterId = chapterId,
                RuleStageIndex = stageIndex,
                RelicChapterId = chapterId,
                RelicStageIndex = stageIndex,
                RelicDifficulty = difficulty,
                Hero = chapters != null ? chapters.GetHeroOfLinear(linear) : null,
                Stage = chapters != null ? chapters.GetStageByLinear(linear) : null,
            };
        }

        //# 도전 층 = 최고 층 + 1 — 시작 시 한 번 확정하고 판 도중 다시 읽지 않는다. 시즌이 바뀌었으면 이전 시즌 진행을 먼저 지운다 (§4.6).
        public static BattleSpec ResolveTower(MetaProfile profile, ChapterDatabase chapters, TowerDatabase tower)
        {
            if (tower == null || chapters == null)
                return null;
            profile.NormalizeTowerSeason(tower.Config.seasonId);
            TowerFloor floor = tower.GetFloor(profile.GetTowerHighestFloor() + 1, DifficultyTable.CurrentMaxIndex);
            HeroArchetypeRow hero = chapters.GetHero(floor.HeroId);
            HeroArchetype_TowerRow towerHero = tower.GetHeroRow(floor.HeroId);
            if (hero == null || towerHero == null)
                return null;
            return new BattleSpec
            {
                Mode = EBattleMode.Tower,
                EffectiveDifficulty = 0,
                LinearStage = StageProgress.ToLinear(floor.HeroChapterId, floor.Position),
                BackdropChapterId = floor.HeroChapterId,
                RuleChapterId = floor.TemplateChapterId,
                RuleStageIndex = floor.TemplateStageIndex,
                RelicChapterId = floor.HeroChapterId,
                RelicStageIndex = floor.Position,
                RelicDifficulty = floor.RelicTier,
                Hero = hero,
                TowerHero = towerHero,
                Floor = floor,
            };
        }
    }
}
