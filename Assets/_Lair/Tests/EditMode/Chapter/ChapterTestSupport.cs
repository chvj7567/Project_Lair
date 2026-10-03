using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 정적 상태(ChapterDatabase.Current, StageProgress 챕터 수) 격리 — 테스트 전 기본값으로 초기화하고 후 원복한다.
    public abstract class ChapterStaticStateTestBase
    {
        private ChapterDatabase _prevCurrent;
        private int _prevChapterCount;

        [SetUp]
        public void 챕터_정적상태를_저장하고_기본값으로_초기화한다()
        {
            _prevCurrent = ChapterDatabase.Current;
            _prevChapterCount = StageProgress.ChapterCount;
            ChapterDatabase.Current = null;
            StageProgress.ConfigureChapterCount(StageProgress.DefaultChapterCount);
        }

        [TearDown]
        public void 챕터_정적상태를_원복한다()
        {
            ChapterDatabase.Current = _prevCurrent;
            StageProgress.ConfigureChapterCount(_prevChapterCount);
        }
    }

    //# 메모리 상의 유효 챕터 데이터 묶음 — shipped JSON 에 의존하지 않고 위반 케이스를 한 곳만 비틀어 만든다.
    public class ChapterDataSet
    {
        public List<ChapterRow> Chapters = new List<ChapterRow>();
        public List<StageRow> Stages = new List<StageRow>();
        public List<Stage_RuleRow> Rules = new List<Stage_RuleRow>();
        public List<Stage_Rule_ParamRow> Params = new List<Stage_Rule_ParamRow>();
        public List<Stage_Rule_TriggerRow> Triggers = new List<Stage_Rule_TriggerRow>();
        public List<HeroArchetypeRow> Heroes = new List<HeroArchetypeRow>();
        public List<HeroArchetype_KiteRow> Kites = new List<HeroArchetype_KiteRow>();

        //# 기획서 §3.4 규칙별 필수 파라미터 키 (production 의 표와 독립으로 다시 적어 둔 기대값).
        public static string[] RequiredKeys(string ruleId)
        {
            switch (ruleId)
            {
                case "HolySpring": return new[] { "centerX", "centerZ", "radius", "healPercentPerSec", "activeSec", "periodSec", "firstActiveSec" };
                case "DenseFog": return new[] { "monsterRangeMul", "minRange" };
                case "SealedNest": return new[] { "lockedSpecies", "unlockHpFraction" };
                case "BossBulwark": return new[] { "damageTakenMul", "durationSec" };
                case "ArrowRain": return new[] { "intervalSec", "searchRadius", "radius", "tickCount", "tickIntervalSec", "damagePerTick" };
                case "HunterTraps": return new[] { "trapCount", "ringDistance", "trapRadius", "lifeSec", "rearmSec", "monsterSpeedMul", "damagePerSec" };
                default: return new string[0];
            }
        }

        public static HeroArchetypeRow Knight()
        {
            return new HeroArchetypeRow { heroId = "Knight", nameKey = 1, hpScale = 1f, powerScale = 1f, range = 1.5f, cooldown = 1f, moveSpeed = 3f, aiType = "Melee" };
        }

        public static HeroArchetypeRow Archer()
        {
            return new HeroArchetypeRow { heroId = "Archer", nameKey = 2, hpScale = 0.8f, powerScale = 0.8f, range = 6f, cooldown = 1f, moveSpeed = 3.2f, aiType = "Kiter" };
        }

        public static HeroArchetype_KiteRow ArcherKite()
        {
            return new HeroArchetype_KiteRow { heroId = "Archer", triggerDist = 3f, resumeDist = 4.5f, threatRadius = 4f, arenaSoftRadius = 7f, corneredCheckSec = 0.5f, corneredMinMove = 0.3f, minDwellSec = 0.4f };
        }

        public void AddChapter(int chapterId, string heroId, string backdropKey)
        {
            Chapters.Add(new ChapterRow { chapterId = chapterId, nameKey = 1000 + chapterId, themeTextKey = 1010 + chapterId, heroId = heroId, backdropKey = backdropKey });
            for (int index = 1; index <= StageProgress.StagesPerChapter; ++index)
            {
                Stages.Add(new StageRow { chapterId = chapterId, stageIndex = index, nameKey = 1100 + index, hpMultiplier = 1f, powerMultiplier = 1f, targetClearSec = 130f });
            }
        }

        //# 규칙 row + 필수 키 전부 + (선택) 트리거. trigger < 0 이면 트리거 row 없음.
        public void AddRule(int chapterId, int stageIndex, int ruleIndex, string ruleId, float trigger = -1f)
        {
            Rules.Add(new Stage_RuleRow { chapterId = chapterId, stageIndex = stageIndex, ruleIndex = ruleIndex, ruleId = ruleId });
            foreach (string key in RequiredKeys(ruleId))
            {
                Params.Add(new Stage_Rule_ParamRow { chapterId = chapterId, stageIndex = stageIndex, ruleIndex = ruleIndex, paramKey = key, value = 1f });
            }
            if (trigger >= 0f)
            {
                Triggers.Add(new Stage_Rule_TriggerRow { chapterId = chapterId, stageIndex = stageIndex, ruleIndex = ruleIndex, hpFraction = trigger });
            }
        }

        //# 챕터 2 + 영웅 2(기사/궁수+카이트) + 규칙 4개(트리거 3개)의 정상 묶음.
        public static ChapterDataSet Valid()
        {
            ChapterDataSet set = new ChapterDataSet();
            set.Heroes.Add(Knight());
            set.Heroes.Add(Archer());
            set.Kites.Add(ArcherKite());
            set.AddChapter(1, "Knight", "Battle_Backdrop");
            set.AddChapter(2, "Archer", "Battle_Backdrop_DarkForest");
            set.AddRule(1, 2, 1, "HolySpring");
            set.AddRule(1, 5, 1, "BossBulwark", 0.55f);
            set.AddRule(2, 5, 1, "ArrowRain", 0.75f);
            set.AddRule(2, 5, 2, "HunterTraps", 0.35f);
            return set;
        }

        public ChapterDatabase Build()
        {
            return ChapterDatabase.Build(
                Chapters.ToArray(), Stages.ToArray(), Rules.ToArray(), Params.ToArray(),
                Triggers.ToArray(), Heroes.ToArray(), Kites.ToArray());
        }
    }
}
