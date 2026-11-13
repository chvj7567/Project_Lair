using System;
using System.Collections.Generic;

namespace Lair.Data
{
    //# 스킬 하나의 수치 묶음 — paramKey → 값. 필수 키 집합은 Build 가 검증하므로 Get 은 항상 값이 있다.
    public sealed class HeroSkillParams
    {
        private readonly Dictionary<string, float> _values;

        public HeroSkillParams(Dictionary<string, float> values)
        {
            _values = values;
        }

        public IReadOnlyDictionary<string, float> Values => _values;

        public float Get(string key) => _values[key];

        //# 정수 의미 값(damage·tickCount 등) — float 로 저장된 값을 반올림해 읽는다.
        public int GetInt(string key) => (int)Math.Round(_values[key]);
    }

    //# HeroSkill 한 행 + 그 스킬의 수치 묶음.
    public sealed class HeroSkillEntry
    {
        public EHeroSkill Id;
        public string DisplayName;
        public HeroSkillParams Params;
    }

    //# HeroArchetype_Skill 한 행 — 영웅의 HP 구간 하나가 여는 스킬.
    public sealed class HeroSkillPhaseEntry
    {
        public int PhaseIndex;
        public float HpFraction;
        public EHeroSkill Skill;
    }

    //# 영웅 스킬 JSON 3종 검증 + 조회 (so-to-json-migration §6.2 2·4번). Unity 비의존 순수 클래스. 위반은 ChapterDataException 으로 전부 모아 던진다.
    public sealed class HeroSkillDatabase
    {
        //# 로드·검증 성공 시에만 채워진다. null 이면 영웅 스킬 미로드 — 전투 진입 불가(§6.4).
        public static HeroSkillDatabase Current { get; set; }

        //# 스킬별 필수 paramKey 집합 (§3.9) — 누락·초과 모두 오류. 값은 스킬 SO 의 [SerializeField] 필드명(앞 '_' 제외)과 같다.
        private static readonly Dictionary<EHeroSkill, string[]> RequiredParams = new Dictionary<EHeroSkill, string[]>
        {
            { EHeroSkill.DashStrike, new[] { "damage", "cooldown", "dashLength", "coneHalfAngle", "knockbackStrength", "centroidRadius" } },
            { EHeroSkill.AoeNova, new[] { "damage", "cooldown", "radius", "knockbackStrength" } },
            { EHeroSkill.OrbitingBlade, new[] { "damage", "hitInterval", "orbitRadius", "bladeSphereRadius", "bladeCount", "rotationSpeedDeg" } },
            { EHeroSkill.PiercingShot, new[] { "damage", "cooldown", "lineLength", "lineHalfWidth", "knockbackStrength", "centroidRadius" } },
            { EHeroSkill.ArrowVolley, new[] { "damagePerTick", "tickCount", "tickIntervalSec", "radius", "cooldown", "targetSearchRadius" } },
            { EHeroSkill.Evade, new[] { "damage", "cooldown", "leapDistance", "leapDurationSec", "blastRadius", "knockbackStrength", "triggerMonsterCount", "triggerRadius" } },
            { EHeroSkill.ShadowStrike, new[] { "damage", "cooldown", "blinkRange", "minBlinkDistance", "arriveOffset", "invulnerableSec", "killRefundFraction" } },
            { EHeroSkill.BladeWhirl, new[] { "damagePerTick", "tickCount", "tickIntervalSec", "radius", "cooldown", "damageTakenMul", "triggerMonsterCount", "triggerRadius" } },
            { EHeroSkill.SmokeBomb, new[] { "radius", "durationSec", "monsterSpeedMul", "monsterRangeMul", "monsterMinRange", "cooldown", "triggerMonsterCount", "triggerRadius" } },
            { EHeroSkill.CrushingLeap, new[] { "damage", "cooldown", "leapRange", "minLeapDistance", "leapDurationSec", "invulnerableSec", "slamRadius", "knockbackStrength", "clusterRadius" } },
            { EHeroSkill.ChainPull, new[] { "damage", "cooldown", "pullRadius", "maxTargets", "pullToDistance", "pullDurationSec", "triggerMonsterCount", "triggerRadius" } },
            { EHeroSkill.BloodFrenzy, new[] { "durationSec", "attackCooldownMul", "damageTakenMul", "cooldown", "triggerMonsterCount", "triggerRadius" } },
            { EHeroSkill.JudgmentHammer, new[] { "damage", "cooldown", "radius", "stunSec", "castRange" } },
            { EHeroSkill.PurifyingLight, new[] { "triggerDebuffCount", "healPercent", "cooldown" } },
            { EHeroSkill.Sanctuary, new[] { "durationSec", "damageTakenMul", "radius", "monsterSpeedMul", "cooldown", "triggerMonsterCount", "triggerRadius" } },
        };

        //# 정수로 읽히는 paramKey — 이 키의 값은 정수여야 한다(스킬 SO 의 int 필드와 같은 집합).
        private static readonly HashSet<string> IntParams = new HashSet<string>
        {
            "bladeCount", "damage", "damagePerTick", "maxTargets", "tickCount", "triggerDebuffCount", "triggerMonsterCount",
        };

        private readonly Dictionary<EHeroSkill, HeroSkillEntry> _skills = new Dictionary<EHeroSkill, HeroSkillEntry>();
        private readonly Dictionary<string, List<HeroSkillPhaseEntry>> _phases = new Dictionary<string, List<HeroSkillPhaseEntry>>();

        private HeroSkillDatabase() { }

        public HeroSkillEntry GetSkill(EHeroSkill id)
        {
            return _skills.TryGetValue(id, out HeroSkillEntry entry) ? entry : null;
        }

        //# 영웅의 HP 구간 목록 — phaseIndex 순(= hpFraction 내림차순). 행이 없는 영웅이면 빈 목록.
        public IReadOnlyList<HeroSkillPhaseEntry> GetPhases(string heroId)
        {
            if (heroId != null && _phases.TryGetValue(heroId, out List<HeroSkillPhaseEntry> list))
                return list;
            return Array.Empty<HeroSkillPhaseEntry>();
        }

        //# chapters 는 heroId FK 와 영웅별 커버리지의 출처 — 챕터 데이터 확정 뒤(§6.2 4번)에 부른다.
        //# nameKeyExists·text 는 Strings_Ko 조회 (Data 가 로더에 기대지 않게 주입).
        public static HeroSkillDatabase Build(HeroSkillDataSet data, ChapterDatabase chapters, Func<int, bool> nameKeyExists, Func<int, string> text)
        {
            if (data == null)
                throw new ChapterDataException("영웅 스킬 데이터가 비어 있음");
            if (chapters == null)
                throw new ChapterDataException("영웅 스킬 검증에 챕터 데이터가 필요함");

            List<string> errors = new List<string>();
            HeroSkillDatabase db = new HeroSkillDatabase();
            db.LoadSkills(data.Skills, nameKeyExists, text, errors);
            db.LoadParams(data.Params, errors);
            db.LoadPhases(data.Phases, chapters, errors);
            if (errors.Count > 0)
                throw new ChapterDataException("영웅 스킬 데이터 검증 실패:\n - " + string.Join("\n - ", errors));
            return db;
        }

        //# PK skillId 중복 0 · skillId 는 EHeroSkill 값명 · nameKey 는 양수이며 Strings_Ko 에 존재(id 0 이 실재해 존재 검사만으로는 0 채움을 못 잡는다) · EHeroSkill 전 값에 행.
        private void LoadSkills(HeroSkillRow[] rows, Func<int, bool> nameKeyExists, Func<int, string> text, List<string> errors)
        {
            foreach (HeroSkillRow row in rows ?? Array.Empty<HeroSkillRow>())
            {
                if (row == null || FormationDatabase.TryParseEnum(row.skillId, out EHeroSkill id) == false)
                {
                    errors.Add($"HeroSkill.json skillId '{row?.skillId}' 가 EHeroSkill 에 없음");
                    continue;
                }
                if (_skills.ContainsKey(id))
                {
                    errors.Add($"HeroSkill.json skillId {id} 중복");
                    continue;
                }
                if (row.nameKey < 1 || nameKeyExists(row.nameKey) == false)
                {
                    errors.Add($"HeroSkill[{id}].nameKey {row.nameKey} 가 Strings_Ko 에 없음");
                }
                _skills.Add(id, new HeroSkillEntry { Id = id, DisplayName = text(row.nameKey) ?? string.Empty });
            }

            foreach (EHeroSkill id in Enum.GetValues(typeof(EHeroSkill)))
            {
                if (_skills.ContainsKey(id) == false)
                {
                    errors.Add($"HeroSkill.json 에 {id} 행이 없음");
                }
            }
        }

        //# 복합 PK(skillId, paramKey) 중복 0 · skillId FK · value 는 0 초과(0 채움 방지)이며 정수 키는 정수 · 스킬별 필수 paramKey 집합과 정확히 일치.
        private void LoadParams(HeroSkill_ParamRow[] rows, List<string> errors)
        {
            Dictionary<EHeroSkill, Dictionary<string, float>> bySkill = new Dictionary<EHeroSkill, Dictionary<string, float>>();
            foreach (HeroSkill_ParamRow row in rows ?? Array.Empty<HeroSkill_ParamRow>())
            {
                if (row == null || FormationDatabase.TryParseEnum(row.skillId, out EHeroSkill id) == false || _skills.ContainsKey(id) == false)
                {
                    errors.Add($"HeroSkill_Param.json skillId '{row?.skillId}' 가 HeroSkill.json 에 없음");
                    continue;
                }
                if (string.IsNullOrEmpty(row.paramKey))
                {
                    errors.Add($"HeroSkill_Param.json {id} 의 paramKey 가 비어 있음");
                    continue;
                }
                if (bySkill.TryGetValue(id, out Dictionary<string, float> values) == false)
                {
                    values = new Dictionary<string, float>();
                    bySkill.Add(id, values);
                }
                if (values.ContainsKey(row.paramKey))
                {
                    errors.Add($"HeroSkill_Param.json 복합키 중복: {id}/{row.paramKey}");
                    continue;
                }
                if (row.value <= 0f || float.IsNaN(row.value))
                {
                    errors.Add($"HeroSkill_Param[{id}/{row.paramKey}].value {row.value} 는 0 보다 커야 함");
                }
                else if (IntParams.Contains(row.paramKey) && row.value != Math.Round(row.value))
                {
                    errors.Add($"HeroSkill_Param[{id}/{row.paramKey}].value {row.value} 는 정수여야 함");
                }
                values.Add(row.paramKey, row.value);
            }

            foreach (KeyValuePair<EHeroSkill, HeroSkillEntry> pair in _skills)
            {
                EHeroSkill id = pair.Key;
                if (RequiredParams.TryGetValue(id, out string[] required) == false)
                {
                    errors.Add($"필수 paramKey 표에 {id} 가 없음");
                    continue;
                }
                bySkill.TryGetValue(id, out Dictionary<string, float> actual);
                actual = actual ?? new Dictionary<string, float>();
                foreach (string key in required)
                {
                    if (actual.ContainsKey(key) == false)
                    {
                        errors.Add($"HeroSkill_Param.json 에 {id}/{key} 가 없음");
                    }
                }
                foreach (string key in actual.Keys)
                {
                    if (Array.IndexOf(required, key) < 0)
                    {
                        errors.Add($"HeroSkill_Param.json {id}/{key} 는 {id} 의 수치가 아님");
                    }
                }
                pair.Value.Params = new HeroSkillParams(actual);
            }
        }

        //# 복합 PK(heroId, phaseIndex) 중복 0 · heroId FK(HeroArchetype) · skillId FK · hpFraction 0 초과 1 미만 ·
        //# 영웅마다 1행 이상, phaseIndex 는 1부터 연속, hpFraction 은 구간이 늘수록 작아진다.
        private void LoadPhases(HeroArchetype_SkillRow[] rows, ChapterDatabase chapters, List<string> errors)
        {
            Dictionary<string, SortedDictionary<int, HeroSkillPhaseEntry>> byHero = new Dictionary<string, SortedDictionary<int, HeroSkillPhaseEntry>>();
            foreach (HeroArchetype_SkillRow row in rows ?? Array.Empty<HeroArchetype_SkillRow>())
            {
                if (row == null || string.IsNullOrEmpty(row.heroId) || chapters.GetHero(row.heroId) == null)
                {
                    errors.Add($"HeroArchetype_Skill.json heroId '{row?.heroId}' 가 HeroArchetype.json 에 없음");
                    continue;
                }
                string label = $"HeroArchetype_Skill[{row.heroId}/{row.phaseIndex}]";
                if (FormationDatabase.TryParseEnum(row.skillId, out EHeroSkill skill) == false || _skills.ContainsKey(skill) == false)
                {
                    errors.Add($"{label}.skillId '{row.skillId}' 가 HeroSkill.json 에 없음");
                    continue;
                }
                if (row.phaseIndex < 1)
                {
                    errors.Add($"{label} phaseIndex 는 1 이상이어야 함");
                    continue;
                }
                if (row.hpFraction <= 0f || row.hpFraction >= 1f)
                {
                    errors.Add($"{label}.hpFraction {row.hpFraction} 는 0 초과 1 미만이어야 함");
                }
                if (byHero.TryGetValue(row.heroId, out SortedDictionary<int, HeroSkillPhaseEntry> phases) == false)
                {
                    phases = new SortedDictionary<int, HeroSkillPhaseEntry>();
                    byHero.Add(row.heroId, phases);
                }
                if (phases.ContainsKey(row.phaseIndex))
                {
                    errors.Add($"HeroArchetype_Skill.json 복합키 중복: {row.heroId}/{row.phaseIndex}");
                    continue;
                }
                phases.Add(row.phaseIndex, new HeroSkillPhaseEntry { PhaseIndex = row.phaseIndex, HpFraction = row.hpFraction, Skill = skill });
            }

            foreach (string heroId in chapters.HeroIds)
            {
                if (byHero.TryGetValue(heroId, out SortedDictionary<int, HeroSkillPhaseEntry> phases) == false)
                {
                    errors.Add($"HeroArchetype_Skill.json 에 영웅 {heroId} 의 행이 없음");
                    continue;
                }
                List<HeroSkillPhaseEntry> ordered = new List<HeroSkillPhaseEntry>();
                int expected = 1;
                foreach (HeroSkillPhaseEntry phase in phases.Values)
                {
                    if (phase.PhaseIndex != expected)
                    {
                        errors.Add($"HeroArchetype_Skill[{heroId}] phaseIndex 는 1부터 연속이어야 함 (기대 {expected}, 실제 {phase.PhaseIndex})");
                        break;
                    }
                    if (ordered.Count > 0 && phase.HpFraction >= ordered[ordered.Count - 1].HpFraction)
                    {
                        errors.Add($"HeroArchetype_Skill[{heroId}/{phase.PhaseIndex}].hpFraction {phase.HpFraction} 는 이전 구간보다 작아야 함");
                    }
                    ordered.Add(phase);
                    expected++;
                }
                _phases[heroId] = ordered;
            }
        }
    }
}
