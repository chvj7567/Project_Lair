using System;
using System.Collections.Generic;
using System.Text;
using Lair.Battle;

namespace Lair.Data
{
    //# 스테이지 규칙 하나 — Stage_Rule + Params + (선택) Trigger 를 합친 읽기 전용 뷰. 규칙 실행은 슬라이스 (b).
    public class StageRuleData
    {
        public int RuleIndex;
        public EStageRule RuleId;
        public IReadOnlyDictionary<string, float> Params;
        public bool HasTrigger;
        public float TriggerHpFraction;
    }

    //# 챕터 데이터 로드 오류 — 검증 위반 전부를 메시지에 모아 던진다(기본값 보정·조용한 폴백 금지, chapter-system §3.1·E-11).
    public class ChapterDataException : Exception
    {
        public ChapterDataException(string message) : base(message) { }
    }

    //# 챕터/스테이지/규칙/영웅 직업 데이터의 정규화 검증 + 조회 (chapter-system §1·§7.3). Unity 비의존 순수 클래스.
    public class ChapterDatabase
    {
        private const float FractionEpsilon = 0.0001f;

        //# 영웅 스킬 게이트 — 보스 페이즈 트리거가 피해야 할 값(chapter-system §3.2). hero_skills.json loadout 과 같은 값.
        private static readonly float[] SkillGateFractions = { 0.85f, 0.65f, 0.45f };

        //# 패시브 카드 임계 — balance_config.json passiveThresholds 와 같은 값(0.9~0.1).
        private static readonly float[] PassiveFractions = { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

        //# 규칙별 필수 파라미터 키 집합 (chapter-system §3.4) — 누락/추가 모두 오류.
        private static readonly Dictionary<EStageRule, string[]> RequiredParams = new Dictionary<EStageRule, string[]>
        {
            { EStageRule.HolySpring, new[] { "centerX", "centerZ", "radius", "healPercentPerSec", "activeSec", "periodSec", "firstActiveSec" } },
            { EStageRule.DenseFog, new[] { "monsterRangeMul", "minRange" } },
            { EStageRule.SealedNest, new[] { "lockedSpecies", "unlockHpFraction" } },
            { EStageRule.BossBulwark, new[] { "damageTakenMul", "durationSec" } },
            { EStageRule.ArrowRain, new[] { "intervalSec", "searchRadius", "radius", "tickCount", "tickIntervalSec", "damagePerTick" } },
            { EStageRule.HunterTraps, new[] { "trapCount", "ringDistance", "trapRadius", "lifeSec", "rearmSec", "monsterSpeedMul", "damagePerSec" } },
        };

        //# 현재 로드된 데이터 — 로더가 채운다. 로드 전/실패 시 null (호출부가 가드).
        public static ChapterDatabase Current { get; set; }

        private readonly List<ChapterRow> _chapters = new List<ChapterRow>();
        private readonly Dictionary<long, StageRow> _stages = new Dictionary<long, StageRow>();
        private readonly Dictionary<string, HeroArchetypeRow> _heroes = new Dictionary<string, HeroArchetypeRow>();
        private readonly Dictionary<string, HeroArchetype_KiteRow> _kites = new Dictionary<string, HeroArchetype_KiteRow>();
        private readonly Dictionary<long, List<StageRuleData>> _rules = new Dictionary<long, List<StageRuleData>>();

        private ChapterDatabase() { }

        public int ChapterCount => _chapters.Count;
        public IReadOnlyList<ChapterRow> Chapters => _chapters;

        //# 검증 후 구축 — 위반이 하나라도 있으면 전부 모아 ChapterDataException.
        public static ChapterDatabase Build(
            ChapterRow[] chapters,
            StageRow[] stages,
            Stage_RuleRow[] rules,
            Stage_Rule_ParamRow[] ruleParams,
            Stage_Rule_TriggerRow[] triggers,
            HeroArchetypeRow[] heroes,
            HeroArchetype_KiteRow[] kites)
        {
            ChapterDatabase db = new ChapterDatabase();
            List<string> errors = new List<string>();

            db.AddHeroes(heroes ?? Array.Empty<HeroArchetypeRow>(), errors);
            db.AddKites(kites ?? Array.Empty<HeroArchetype_KiteRow>(), errors);
            db.AddChapters(chapters ?? Array.Empty<ChapterRow>(), errors);
            db.AddStages(stages ?? Array.Empty<StageRow>(), errors);
            db.AddRules(
                rules ?? Array.Empty<Stage_RuleRow>(),
                ruleParams ?? Array.Empty<Stage_Rule_ParamRow>(),
                triggers ?? Array.Empty<Stage_Rule_TriggerRow>(),
                errors);

            if (errors.Count > 0)
            {
                StringBuilder sb = new StringBuilder("챕터 데이터 검증 실패:");
                foreach (string error in errors)
                {
                    sb.Append("\n - ").Append(error);
                }
                throw new ChapterDataException(sb.ToString());
            }
            return db;
        }

        public ChapterRow GetChapter(int chapterId)
        {
            foreach (ChapterRow chapter in _chapters)
            {
                if (chapter.chapterId == chapterId)
                    return chapter;
            }
            return null;
        }

        public StageRow GetStage(int chapterId, int stageIndex)
        {
            return _stages.TryGetValue(Key(chapterId, stageIndex), out StageRow row) ? row : null;
        }

        //# 선형 스테이지 번호로 조회 — 범위 밖이면 null.
        public StageRow GetStageByLinear(int linear)
        {
            if (linear < 1)
                return null;
            return GetStage(StageProgress.ChapterOf(linear), StageProgress.IndexOf(linear));
        }

        public HeroArchetypeRow GetHero(string heroId)
        {
            return heroId != null && _heroes.TryGetValue(heroId, out HeroArchetypeRow row) ? row : null;
        }

        //# Kiter 영웅의 카이팅 설정 — 없으면 null.
        public HeroArchetype_KiteRow GetKite(string heroId)
        {
            return heroId != null && _kites.TryGetValue(heroId, out HeroArchetype_KiteRow row) ? row : null;
        }

        //# 선형 스테이지 번호가 속한 챕터의 영웅 직업 — 챕터/영웅이 없으면 null.
        public HeroArchetypeRow GetHeroOfLinear(int linear)
        {
            ChapterRow chapter = GetChapter(StageProgress.ChapterOf(linear));
            return chapter != null ? GetHero(chapter.heroId) : null;
        }

        //# 스테이지에 부착된 규칙 목록(ruleIndex 오름차순). 규칙 없는 스테이지는 빈 목록.
        public IReadOnlyList<StageRuleData> GetRules(int chapterId, int stageIndex)
        {
            return _rules.TryGetValue(Key(chapterId, stageIndex), out List<StageRuleData> list)
                ? list
                : (IReadOnlyList<StageRuleData>)Array.Empty<StageRuleData>();
        }

        private static long Key(int a, int b) => ((long)a << 32) | (uint)b;
        private static long Key(int a, int b, int c) => ((long)a << 40) | ((long)b << 20) | (uint)c;

        private void AddHeroes(HeroArchetypeRow[] rows, List<string> errors)
        {
            foreach (HeroArchetypeRow row in rows)
            {
                if (Enum.TryParse(row.heroId, out EHero _) == false)
                {
                    errors.Add($"HeroArchetype.heroId 가 EHero 에 없음: {row.heroId}");
                }
                if (Enum.TryParse(row.aiType, out EHeroAi _) == false)
                {
                    errors.Add($"HeroArchetype[{row.heroId}].aiType 가 EHeroAi 에 없음: {row.aiType}");
                }
                if (row.heroId == null || _heroes.ContainsKey(row.heroId))
                {
                    errors.Add($"HeroArchetype.heroId 중복/누락: {row.heroId}");
                    continue;
                }
                _heroes.Add(row.heroId, row);
            }
        }

        private void AddKites(HeroArchetype_KiteRow[] rows, List<string> errors)
        {
            foreach (HeroArchetype_KiteRow row in rows)
            {
                if (row.heroId == null || _kites.ContainsKey(row.heroId))
                {
                    errors.Add($"HeroArchetype_Kite.heroId 중복/누락: {row.heroId}");
                    continue;
                }
                _kites.Add(row.heroId, row);
            }

            foreach (KeyValuePair<string, HeroArchetype_KiteRow> pair in _kites)
            {
                if (_heroes.TryGetValue(pair.Key, out HeroArchetypeRow hero) == false)
                {
                    errors.Add($"HeroArchetype_Kite[{pair.Key}] 의 heroId 가 HeroArchetype 에 없음");
                }
                else if (hero.aiType != EHeroAi.Kiter.ToString())
                {
                    errors.Add($"HeroArchetype_Kite[{pair.Key}] 는 aiType=Kiter 영웅에만 존재해야 함 (현재 {hero.aiType})");
                }
            }

            foreach (KeyValuePair<string, HeroArchetypeRow> pair in _heroes)
            {
                if (pair.Value.aiType == EHeroAi.Kiter.ToString() && _kites.ContainsKey(pair.Key) == false)
                {
                    errors.Add($"HeroArchetype[{pair.Key}] 는 Kiter 인데 HeroArchetype_Kite row 가 없음");
                }
            }
        }

        private void AddChapters(ChapterRow[] rows, List<string> errors)
        {
            HashSet<int> seen = new HashSet<int>();
            foreach (ChapterRow row in rows)
            {
                if (seen.Add(row.chapterId) == false)
                {
                    errors.Add($"Chapter.chapterId 중복: {row.chapterId}");
                    continue;
                }
                if (GetHero(row.heroId) == null)
                {
                    errors.Add($"Chapter[{row.chapterId}].heroId 가 HeroArchetype 에 없음: {row.heroId}");
                }
                if (Enum.TryParse(row.backdropKey, out EBackdrop _) == false)
                {
                    errors.Add($"Chapter[{row.chapterId}].backdropKey 가 EBackdrop 에 없음: {row.backdropKey}");
                }
                _chapters.Add(row);
            }

            _chapters.Sort((a, b) => a.chapterId.CompareTo(b.chapterId));
            if (_chapters.Count == 0)
            {
                errors.Add("Chapter.json 이 비어 있음");
            }
            for (int i = 0; i < _chapters.Count; ++i)
            {
                if (_chapters[i].chapterId != i + 1)
                {
                    errors.Add($"Chapter.chapterId 는 1부터 연속이어야 함 (위치 {i + 1} 에 {_chapters[i].chapterId})");
                    break;
                }
            }
        }

        private void AddStages(StageRow[] rows, List<string> errors)
        {
            foreach (StageRow row in rows)
            {
                if (GetChapter(row.chapterId) == null)
                {
                    errors.Add($"Stage[{row.chapterId}-{row.stageIndex}] 의 chapterId 가 Chapter 에 없음");
                    continue;
                }
                if (row.stageIndex < 1 || row.stageIndex > StageProgress.StagesPerChapter)
                {
                    errors.Add($"Stage[{row.chapterId}-{row.stageIndex}] stageIndex 범위(1~{StageProgress.StagesPerChapter}) 밖");
                    continue;
                }
                if (_stages.ContainsKey(Key(row.chapterId, row.stageIndex)))
                {
                    errors.Add($"Stage 복합키 중복: {row.chapterId}-{row.stageIndex}");
                    continue;
                }
                _stages.Add(Key(row.chapterId, row.stageIndex), row);
            }

            foreach (ChapterRow chapter in _chapters)
            {
                for (int index = 1; index <= StageProgress.StagesPerChapter; ++index)
                {
                    if (GetStage(chapter.chapterId, index) == null)
                    {
                        errors.Add($"챕터 {chapter.chapterId} 의 스테이지 {index} 가 없음 (챕터당 정확히 {StageProgress.StagesPerChapter}개)");
                    }
                }
            }
        }

        private void AddRules(Stage_RuleRow[] rules, Stage_Rule_ParamRow[] ruleParams, Stage_Rule_TriggerRow[] triggers, List<string> errors)
        {
            Dictionary<long, Stage_RuleRow> ruleRows = new Dictionary<long, Stage_RuleRow>();
            Dictionary<long, Dictionary<string, float>> paramMap = new Dictionary<long, Dictionary<string, float>>();
            Dictionary<long, float> triggerMap = new Dictionary<long, float>();

            foreach (Stage_RuleRow row in rules)
            {
                long key = Key(row.chapterId, row.stageIndex, row.ruleIndex);
                if (GetStage(row.chapterId, row.stageIndex) == null)
                {
                    errors.Add($"Stage_Rule[{row.chapterId}-{row.stageIndex}-{row.ruleIndex}] 의 스테이지가 Stage 에 없음");
                    continue;
                }
                if (ruleRows.ContainsKey(key))
                {
                    errors.Add($"Stage_Rule 복합키 중복: {row.chapterId}-{row.stageIndex}-{row.ruleIndex}");
                    continue;
                }
                if (Enum.TryParse(row.ruleId, out EStageRule _) == false)
                {
                    errors.Add($"Stage_Rule[{row.chapterId}-{row.stageIndex}-{row.ruleIndex}].ruleId 가 EStageRule 에 없음: {row.ruleId}");
                    continue;
                }
                ruleRows.Add(key, row);
                paramMap.Add(key, new Dictionary<string, float>());
            }

            foreach (Stage_Rule_ParamRow row in ruleParams)
            {
                long key = Key(row.chapterId, row.stageIndex, row.ruleIndex);
                if (paramMap.TryGetValue(key, out Dictionary<string, float> map) == false)
                {
                    errors.Add($"Stage_Rule_Param[{row.chapterId}-{row.stageIndex}-{row.ruleIndex}] 의 규칙이 Stage_Rule 에 없음");
                    continue;
                }
                if (map.ContainsKey(row.paramKey))
                {
                    errors.Add($"Stage_Rule_Param 복합키 중복: {row.chapterId}-{row.stageIndex}-{row.ruleIndex}/{row.paramKey}");
                    continue;
                }
                map.Add(row.paramKey, row.value);
            }

            foreach (Stage_Rule_TriggerRow row in triggers)
            {
                long key = Key(row.chapterId, row.stageIndex, row.ruleIndex);
                string label = $"{row.chapterId}-{row.stageIndex}-{row.ruleIndex}";
                if (ruleRows.ContainsKey(key) == false)
                {
                    errors.Add($"Stage_Rule_Trigger[{label}] 의 규칙이 Stage_Rule 에 없음");
                    continue;
                }
                if (triggerMap.ContainsKey(key))
                {
                    errors.Add($"Stage_Rule_Trigger 복합키 중복: {label}");
                    continue;
                }
                if (MatchesAny(row.hpFraction, SkillGateFractions) || MatchesAny(row.hpFraction, PassiveFractions))
                {
                    errors.Add($"Stage_Rule_Trigger[{label}].hpFraction {row.hpFraction} 가 스킬 게이트/패시브 임계와 일치 (0.05 이격 필요)");
                }
                triggerMap.Add(key, row.hpFraction);
            }

            foreach (KeyValuePair<long, Stage_RuleRow> pair in ruleRows)
            {
                Stage_RuleRow row = pair.Value;
                EStageRule ruleId = (EStageRule)Enum.Parse(typeof(EStageRule), row.ruleId);
                Dictionary<string, float> map = paramMap[pair.Key];
                ValidateRequiredParams(row, ruleId, map, errors);

                bool hasTrigger = triggerMap.TryGetValue(pair.Key, out float fraction);
                StageRuleData data = new StageRuleData
                {
                    RuleIndex = row.ruleIndex,
                    RuleId = ruleId,
                    Params = map,
                    HasTrigger = hasTrigger,
                    TriggerHpFraction = hasTrigger ? fraction : 0f,
                };
                long stageKey = Key(row.chapterId, row.stageIndex);
                if (_rules.TryGetValue(stageKey, out List<StageRuleData> list) == false)
                {
                    list = new List<StageRuleData>();
                    _rules.Add(stageKey, list);
                }
                list.Add(data);
            }

            foreach (List<StageRuleData> list in _rules.Values)
            {
                list.Sort((a, b) => a.RuleIndex.CompareTo(b.RuleIndex));
            }
        }

        private static void ValidateRequiredParams(Stage_RuleRow row, EStageRule ruleId, Dictionary<string, float> map, List<string> errors)
        {
            string label = $"{row.chapterId}-{row.stageIndex}-{row.ruleIndex}({row.ruleId})";
            string[] required = RequiredParams[ruleId];
            foreach (string key in required)
            {
                if (map.ContainsKey(key) == false)
                {
                    errors.Add($"Stage_Rule_Param[{label}] 필수 키 누락: {key}");
                }
            }
            foreach (string key in map.Keys)
            {
                if (Array.IndexOf(required, key) < 0)
                {
                    errors.Add($"Stage_Rule_Param[{label}] 허용되지 않은 추가 키: {key}");
                }
            }
        }

        private static bool MatchesAny(float value, float[] candidates)
        {
            foreach (float candidate in candidates)
            {
                if (Math.Abs(value - candidate) < FractionEpsilon)
                    return true;
            }
            return false;
        }
    }
}
