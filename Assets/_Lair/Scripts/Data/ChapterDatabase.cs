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

        //# 영웅 강화 외형 티어 상한(Stage.heroTier 0~4) — 0 은 오버레이 꺼짐.
        private const int MaxHeroTier = 4;

        //# 현재 로드된 데이터 — 로더가 채운다. 로드 전/실패 시 null (호출부가 가드).
        public static ChapterDatabase Current { get; set; }

        private readonly List<ChapterRow> _chapters = new List<ChapterRow>();
        private readonly Dictionary<long, StageRow> _stages = new Dictionary<long, StageRow>();
        private readonly Dictionary<string, HeroArchetypeRow> _heroes = new Dictionary<string, HeroArchetypeRow>();
        private readonly Dictionary<string, HeroArchetype_KiteRow> _kites = new Dictionary<string, HeroArchetype_KiteRow>();
        private readonly Dictionary<string, HeroArchetype_SkirmishRow> _skirmishes = new Dictionary<string, HeroArchetype_SkirmishRow>();
        private readonly Dictionary<string, HeroArchetype_ChargeRow> _charges = new Dictionary<string, HeroArchetype_ChargeRow>();
        private readonly Dictionary<string, HeroArchetype_CleaveRow> _cleaves = new Dictionary<string, HeroArchetype_CleaveRow>();
        private readonly Dictionary<string, HeroArchetype_GuardRow> _guards = new Dictionary<string, HeroArchetype_GuardRow>();
        private readonly Dictionary<string, HeroArchetype_DebuffResistRow> _debuffResists = new Dictionary<string, HeroArchetype_DebuffResistRow>();
        private readonly Dictionary<long, List<StageRuleData>> _rules = new Dictionary<long, List<StageRuleData>>();

        private ChapterDatabase() { }

        public int ChapterCount => _chapters.Count;
        public IReadOnlyList<ChapterRow> Chapters => _chapters;

        //# 검증 후 구축 — 위반이 하나라도 있으면 전부 모아 ChapterDataException.
        //# passiveThresholds·skillGateFractions = 보스 트리거가 피해야 할 패시브 임계(BalanceConfig)·스킬 해금 HP 비율(HeroArchetype_Skill, §6.3). null 이면 그 대조만 생략.
        public static ChapterDatabase Build(
            ChapterRow[] chapters,
            StageRow[] stages,
            Stage_RuleRow[] rules,
            Stage_Rule_ParamRow[] ruleParams,
            Stage_Rule_TriggerRow[] triggers,
            HeroArchetypeRow[] heroes,
            HeroArchetype_KiteRow[] kites,
            HeroArchetype_SkirmishRow[] skirmishes,
            HeroArchetype_ChargeRow[] charges = null,
            HeroArchetype_CleaveRow[] cleaves = null,
            HeroArchetype_GuardRow[] guards = null,
            HeroArchetype_DebuffResistRow[] debuffResists = null,
            float[] passiveThresholds = null,
            float[] skillGateFractions = null)
        {
            ChapterDatabase db = new ChapterDatabase();
            List<string> errors = new List<string>();

            db.AddHeroes(heroes ?? Array.Empty<HeroArchetypeRow>(), errors);
            db.AddKites(kites ?? Array.Empty<HeroArchetype_KiteRow>(), errors);
            db.AddSkirmishes(skirmishes ?? Array.Empty<HeroArchetype_SkirmishRow>(), errors);
            db.AddCharges(charges ?? Array.Empty<HeroArchetype_ChargeRow>(), errors);
            db.AddCleaves(cleaves ?? Array.Empty<HeroArchetype_CleaveRow>(), errors);
            db.AddGuards(guards ?? Array.Empty<HeroArchetype_GuardRow>(), errors);
            db.AddDebuffResists(debuffResists ?? Array.Empty<HeroArchetype_DebuffResistRow>(), errors);
            db.AddChapters(chapters ?? Array.Empty<ChapterRow>(), errors);
            db.AddStages(stages ?? Array.Empty<StageRow>(), errors);
            db.AddRules(
                rules ?? Array.Empty<Stage_RuleRow>(),
                ruleParams ?? Array.Empty<Stage_Rule_ParamRow>(),
                triggers ?? Array.Empty<Stage_Rule_TriggerRow>(),
                passiveThresholds ?? Array.Empty<float>(),
                skillGateFractions ?? Array.Empty<float>(),
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

        //# 선형 번호로 조회하되 범위 밖(탑·데일리 등)은 [1, 챕터수×5] 로 클램프 — 영웅 외형(heroScale·heroTier) 조회용.
        public StageRow GetStageClamped(int linear)
        {
            return GetStageByLinear(Math.Max(1, Math.Min(linear, ChapterCount * StageProgress.StagesPerChapter)));
        }

        //# 영웅 직업 전체 heroId — 영웅별 데이터 커버리지 검증용(영웅 스킬 구간 등).
        public IReadOnlyCollection<string> HeroIds => _heroes.Keys;

        public HeroArchetypeRow GetHero(string heroId)
        {
            return heroId != null && _heroes.TryGetValue(heroId, out HeroArchetypeRow row) ? row : null;
        }

        //# Kiter 영웅의 카이팅 설정 — 없으면 null.
        public HeroArchetype_KiteRow GetKite(string heroId)
        {
            return heroId != null && _kites.TryGetValue(heroId, out HeroArchetype_KiteRow row) ? row : null;
        }

        //# Skirmisher 영웅의 습격-이탈 설정 — 없으면 null.
        public HeroArchetype_SkirmishRow GetSkirmish(string heroId)
        {
            return heroId != null && _skirmishes.TryGetValue(heroId, out HeroArchetype_SkirmishRow row) ? row : null;
        }

        //# Charger 영웅의 군집 돌진 설정 — 없으면 null.
        public HeroArchetype_ChargeRow GetCharge(string heroId)
        {
            return heroId != null && _charges.TryGetValue(heroId, out HeroArchetype_ChargeRow row) ? row : null;
        }

        //# 범위 베기 기본 공격 영웅의 베기 설정 — 없으면 null(단일 판정).
        public HeroArchetype_CleaveRow GetCleave(string heroId)
        {
            return heroId != null && _cleaves.TryGetValue(heroId, out HeroArchetype_CleaveRow row) ? row : null;
        }

        //# Guardian 영웅의 응징 표적 설정 — 없으면 null.
        public HeroArchetype_GuardRow GetGuard(string heroId)
        {
            return heroId != null && _guards.TryGetValue(heroId, out HeroArchetype_GuardRow row) ? row : null;
        }

        //# 디버프 저항 패시브 영웅의 저항 설정 — 없으면 null(저항 없음).
        public HeroArchetype_DebuffResistRow GetDebuffResist(string heroId)
        {
            return heroId != null && _debuffResists.TryGetValue(heroId, out HeroArchetype_DebuffResistRow row) ? row : null;
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
                if (IsEnumName<EHero>(row.heroId) == false)
                {
                    errors.Add($"HeroArchetype.heroId 가 EHero 에 없음: {row.heroId}");
                }
                if (IsEnumName<EHeroAi>(row.aiType) == false)
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

        private void AddSkirmishes(HeroArchetype_SkirmishRow[] rows, List<string> errors)
        {
            foreach (HeroArchetype_SkirmishRow row in rows)
            {
                if (row.heroId == null || _skirmishes.ContainsKey(row.heroId))
                {
                    errors.Add($"HeroArchetype_Skirmish.heroId 중복/누락: {row.heroId}");
                    continue;
                }
                _skirmishes.Add(row.heroId, row);
            }

            foreach (KeyValuePair<string, HeroArchetype_SkirmishRow> pair in _skirmishes)
            {
                if (_heroes.TryGetValue(pair.Key, out HeroArchetypeRow hero) == false)
                {
                    errors.Add($"HeroArchetype_Skirmish[{pair.Key}] 의 heroId 가 HeroArchetype 에 없음");
                }
                else if (hero.aiType != EHeroAi.Skirmisher.ToString())
                {
                    errors.Add($"HeroArchetype_Skirmish[{pair.Key}] 는 aiType=Skirmisher 영웅에만 존재해야 함 (현재 {hero.aiType})");
                }
            }

            foreach (KeyValuePair<string, HeroArchetypeRow> pair in _heroes)
            {
                if (pair.Value.aiType == EHeroAi.Skirmisher.ToString() && _skirmishes.ContainsKey(pair.Key) == false)
                {
                    errors.Add($"HeroArchetype[{pair.Key}] 는 Skirmisher 인데 HeroArchetype_Skirmish row 가 없음");
                }
            }
        }

        private void AddCharges(HeroArchetype_ChargeRow[] rows, List<string> errors)
        {
            foreach (HeroArchetype_ChargeRow row in rows)
            {
                if (row.heroId == null || _charges.ContainsKey(row.heroId))
                {
                    errors.Add($"HeroArchetype_Charge.heroId 중복/누락: {row.heroId}");
                    continue;
                }
                _charges.Add(row.heroId, row);
            }

            foreach (KeyValuePair<string, HeroArchetype_ChargeRow> pair in _charges)
            {
                HeroArchetype_ChargeRow row = pair.Value;
                if (_heroes.TryGetValue(pair.Key, out HeroArchetypeRow hero) == false)
                {
                    errors.Add($"HeroArchetype_Charge[{pair.Key}] 의 heroId 가 HeroArchetype 에 없음");
                }
                else if (hero.aiType != EHeroAi.Charger.ToString())
                {
                    errors.Add($"HeroArchetype_Charge[{pair.Key}] 는 aiType=Charger 영웅에만 존재해야 함 (현재 {hero.aiType})");
                }
                if (row.huntRadius <= 0f || row.clusterRadius <= 0f || row.arenaSoftRadius <= 0f)
                {
                    errors.Add($"HeroArchetype_Charge[{pair.Key}] 반경(huntRadius/clusterRadius/arenaSoftRadius)은 0 보다 커야 함");
                }
                if (row.retargetCountMargin < 1 || row.retargetIntervalSec <= 0f || row.minDwellSec < 0f)
                {
                    errors.Add($"HeroArchetype_Charge[{pair.Key}] retargetCountMargin>=1, retargetIntervalSec>0, minDwellSec>=0 이어야 함");
                }
            }

            foreach (KeyValuePair<string, HeroArchetypeRow> pair in _heroes)
            {
                if (pair.Value.aiType == EHeroAi.Charger.ToString() && _charges.ContainsKey(pair.Key) == false)
                {
                    errors.Add($"HeroArchetype[{pair.Key}] 는 Charger 인데 HeroArchetype_Charge row 가 없음");
                }
            }
        }

        private void AddCleaves(HeroArchetype_CleaveRow[] rows, List<string> errors)
        {
            foreach (HeroArchetype_CleaveRow row in rows)
            {
                if (row.heroId == null || _cleaves.ContainsKey(row.heroId))
                {
                    errors.Add($"HeroArchetype_Cleave.heroId 중복/누락: {row.heroId}");
                    continue;
                }
                _cleaves.Add(row.heroId, row);
            }

            foreach (KeyValuePair<string, HeroArchetype_CleaveRow> pair in _cleaves)
            {
                HeroArchetype_CleaveRow row = pair.Value;
                if (_heroes.ContainsKey(pair.Key) == false)
                {
                    errors.Add($"HeroArchetype_Cleave[{pair.Key}] 의 heroId 가 HeroArchetype 에 없음");
                }
                if (row.cleaveRadius <= 0f || row.cleaveMaxTargets < 1 || row.cleaveSecondaryMul < 0f || row.cleaveSecondaryMul > 1f)
                {
                    errors.Add($"HeroArchetype_Cleave[{pair.Key}] cleaveRadius>0, cleaveMaxTargets>=1, cleaveSecondaryMul 0~1 이어야 함");
                }
            }
        }

        private void AddGuards(HeroArchetype_GuardRow[] rows, List<string> errors)
        {
            foreach (HeroArchetype_GuardRow row in rows)
            {
                if (row.heroId == null || _guards.ContainsKey(row.heroId))
                {
                    errors.Add($"HeroArchetype_Guard.heroId 중복/누락: {row.heroId}");
                    continue;
                }
                _guards.Add(row.heroId, row);
            }

            foreach (KeyValuePair<string, HeroArchetype_GuardRow> pair in _guards)
            {
                if (_heroes.TryGetValue(pair.Key, out HeroArchetypeRow hero) == false)
                {
                    errors.Add($"HeroArchetype_Guard[{pair.Key}] 의 heroId 가 HeroArchetype 에 없음");
                }
                else if (hero.aiType != EHeroAi.Guardian.ToString())
                {
                    errors.Add($"HeroArchetype_Guard[{pair.Key}] 는 aiType=Guardian 영웅에만 존재해야 함 (현재 {hero.aiType})");
                }
                if (pair.Value.retributionLockSec <= 0f)
                {
                    errors.Add($"HeroArchetype_Guard[{pair.Key}] retributionLockSec 는 0 보다 커야 함");
                }
            }

            foreach (KeyValuePair<string, HeroArchetypeRow> pair in _heroes)
            {
                if (pair.Value.aiType == EHeroAi.Guardian.ToString() && _guards.ContainsKey(pair.Key) == false)
                {
                    errors.Add($"HeroArchetype[{pair.Key}] 는 Guardian 인데 HeroArchetype_Guard row 가 없음");
                }
            }
        }

        private void AddDebuffResists(HeroArchetype_DebuffResistRow[] rows, List<string> errors)
        {
            foreach (HeroArchetype_DebuffResistRow row in rows)
            {
                if (row.heroId == null || _debuffResists.ContainsKey(row.heroId))
                {
                    errors.Add($"HeroArchetype_DebuffResist.heroId 중복/누락: {row.heroId}");
                    continue;
                }
                _debuffResists.Add(row.heroId, row);
            }

            foreach (KeyValuePair<string, HeroArchetype_DebuffResistRow> pair in _debuffResists)
            {
                HeroArchetype_DebuffResistRow row = pair.Value;
                if (_heroes.ContainsKey(pair.Key) == false)
                {
                    errors.Add($"HeroArchetype_DebuffResist[{pair.Key}] 의 heroId 가 HeroArchetype 에 없음");
                }
                if (row.durationMul <= 0f || row.durationMul > 1f || row.penaltyMul < 0f || row.penaltyMul > 1f)
                {
                    errors.Add($"HeroArchetype_DebuffResist[{pair.Key}] durationMul 은 0 초과 1 이하, penaltyMul 은 0 이상 1 이하여야 함");
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
                if (IsEnumName<EBackdrop>(row.backdropKey) == false)
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
                //# JsonUtility 는 누락 필드를 0 으로 채운다 — heroScale 0 이 누락 신호. heroTier 0 은 유효값이라 누락을 못 잡는다(출하 값은 테스트가 대조).
                if (row.heroScale <= 0f || float.IsNaN(row.heroScale))
                {
                    errors.Add($"Stage[{row.chapterId}-{row.stageIndex}].heroScale {row.heroScale} 는 0 보다 커야 함");
                }
                if (row.heroTier < 0 || row.heroTier > MaxHeroTier)
                {
                    errors.Add($"Stage[{row.chapterId}-{row.stageIndex}].heroTier {row.heroTier} 는 0~{MaxHeroTier} 범위 밖");
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

        private void AddRules(Stage_RuleRow[] rules, Stage_Rule_ParamRow[] ruleParams, Stage_Rule_TriggerRow[] triggers, float[] passiveThresholds, float[] skillGateFractions, List<string> errors)
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
                if (IsEnumName<EStageRule>(row.ruleId) == false)
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
                if (MatchesAny(row.hpFraction, skillGateFractions) || MatchesAny(row.hpFraction, passiveThresholds))
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

        //# 값명 문자열만 허용 — Enum.TryParse 는 "1" 같은 숫자 문자열도 통과시키므로 IsDefined 를 함께 건다.
        private static bool IsEnumName<T>(string name) where T : struct, Enum
        {
            return Enum.TryParse(name, out T parsed) && Enum.IsDefined(typeof(T), parsed) && parsed.ToString() == name;
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
