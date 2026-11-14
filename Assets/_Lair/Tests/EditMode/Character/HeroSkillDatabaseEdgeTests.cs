using System;
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# HeroSkillDatabase.Build 검증 경계·조회 API (so-to-json-migration §3.9·§6.3·§10 SJ3~SJ5). 출하 JSON 행을 한 곳씩 비틀어 위반을 만든다.
    public class HeroSkillDatabaseEdgeTests
    {
        //# 스킬별 필수 paramKey — 기획서 §3.9 표를 production 표와 독립으로 다시 적은 기대값(검산 97).
        public static readonly Dictionary<EHeroSkill, string[]> RequiredKeys = new Dictionary<EHeroSkill, string[]>
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

        //# 영웅 → 구간별 스킬 (§4.7). 구간 HP 비율은 모든 영웅이 0.85 · 0.65 · 0.45.
        private static readonly Dictionary<string, EHeroSkill[]> HeroLoadouts = new Dictionary<string, EHeroSkill[]>
        {
            { "Knight", new[] { EHeroSkill.DashStrike, EHeroSkill.AoeNova, EHeroSkill.OrbitingBlade } },
            { "Archer", new[] { EHeroSkill.PiercingShot, EHeroSkill.ArrowVolley, EHeroSkill.Evade } },
            { "Assassin", new[] { EHeroSkill.ShadowStrike, EHeroSkill.BladeWhirl, EHeroSkill.SmokeBomb } },
            { "Berserker", new[] { EHeroSkill.CrushingLeap, EHeroSkill.ChainPull, EHeroSkill.BloodFrenzy } },
            { "Paladin", new[] { EHeroSkill.JudgmentHammer, EHeroSkill.PurifyingLight, EHeroSkill.Sanctuary } },
        };

        private static readonly float[] PhaseFractions = { 0.85f, 0.65f, 0.45f };

        private ChapterDatabase _chapters;
        private HeroSkillDatabase _shipped;

        [OneTimeSetUp]
        public void 챕터_DB와_출하_스킬_DB를_한_번_조립한다()
        {
            _chapters = WorldMapTestSupport.FullDb();
            _shipped = HeroSkillDatabase.Build(WorldMapTestSupport.ShippedHeroSkillData(), _chapters, Exists, WorldMapTestSupport.Text);
        }

        private static bool Exists(int id) => WorldMapTestSupport.Strings().ContainsKey(id);

        private HeroSkillDatabase Build(HeroSkillDataSet data, Func<int, bool> exists = null)
        {
            Func<int, bool> existsCheck = exists != null ? exists : new Func<int, bool>(Exists);
            return HeroSkillDatabase.Build(data, _chapters, existsCheck, WorldMapTestSupport.Text);
        }

        private void AssertFails(HeroSkillDataSet data, params string[] fragments)
        {
            BuildEdgeTestKit.AssertFails(() => Build(data), fragments);
        }

        private static IEnumerable<EHeroSkill> AllSkills()
        {
            foreach (EHeroSkill id in Enum.GetValues(typeof(EHeroSkill)))
            {
                yield return id;
            }
        }

        private static string Pascal(string key) => char.ToUpperInvariant(key[0]) + key.Substring(1);

        private static void RemoveParam(HeroSkillDataSet data, EHeroSkill skill, string key)
        {
            List<HeroSkill_ParamRow> rows = new List<HeroSkill_ParamRow>(data.Params);
            rows.RemoveAll(row => row.skillId == skill.ToString() && row.paramKey == key);
            data.Params = rows.ToArray();
        }

        private static HeroSkill_ParamRow FindParam(HeroSkillDataSet data, EHeroSkill skill, string key)
        {
            return Array.Find(data.Params, row => row.skillId == skill.ToString() && row.paramKey == key);
        }

        private static void AddParam(HeroSkillDataSet data, string skillId, string paramKey, float value)
        {
            List<HeroSkill_ParamRow> rows = new List<HeroSkill_ParamRow>(data.Params);
            rows.Add(new HeroSkill_ParamRow { skillId = skillId, paramKey = paramKey, value = value });
            data.Params = rows.ToArray();
        }

        private static void RemovePhases(HeroSkillDataSet data, Predicate<HeroArchetype_SkillRow> match)
        {
            List<HeroArchetype_SkillRow> rows = new List<HeroArchetype_SkillRow>(data.Phases);
            rows.RemoveAll(match);
            data.Phases = rows.ToArray();
        }

        private static HeroArchetype_SkillRow FindPhase(HeroSkillDataSet data, string heroId, int phaseIndex)
        {
            return Array.Find(data.Phases, row => row.heroId == heroId && row.phaseIndex == phaseIndex);
        }

        //# --- 출하 데이터 구조 ---

        [Test]
        public void 필수_수치_표_합계는_97이고_EHeroSkill_15종_전부를_덮는다()
        {
            int total = 0;
            foreach (EHeroSkill id in AllSkills())
            {
                Assert.IsTrue(RequiredKeys.ContainsKey(id), id.ToString());
                total += RequiredKeys[id].Length;
            }

            Assert.AreEqual(15, RequiredKeys.Count);
            Assert.AreEqual(97, total);
        }

        [TestCaseSource(nameof(AllSkills))]
        public void 출하_스킬의_수치_키_집합은_기획서_표와_정확히_같다(EHeroSkill id)
        {
            HeroSkillEntry entry = _shipped.GetSkill(id);

            CollectionAssert.AreEquivalent(RequiredKeys[id], new List<string>(entry.Params.Values.Keys), id.ToString());
        }

        [Test]
        public void 출하_영웅_5명의_구간은_기획서_표_스킬과_HP_비율_순서다()
        {
            CollectionAssert.AreEquivalent(HeroLoadouts.Keys, _chapters.HeroIds);
            foreach (KeyValuePair<string, EHeroSkill[]> pair in HeroLoadouts)
            {
                IReadOnlyList<HeroSkillPhaseEntry> phases = _shipped.GetPhases(pair.Key);

                Assert.AreEqual(3, phases.Count, pair.Key);
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(i + 1, phases[i].PhaseIndex, pair.Key + " index");
                    Assert.AreEqual(PhaseFractions[i], phases[i].HpFraction, 0f, pair.Key + " fraction");
                    Assert.AreEqual(pair.Value[i], phases[i].Skill, pair.Key + " skill");
                }
            }
        }

        [Test]
        public void 출하_스킬_이름은_Strings_문구와_같다()
        {
            for (int i = 0; i < 15; i++)
            {
                EHeroSkill id = (EHeroSkill)i;

                Assert.AreEqual(WorldMapTestSupport.Text(1617 + i), _shipped.GetSkill(id).DisplayName, id.ToString());
            }
        }

        //# --- 필수 입력 ---

        [Test]
        public void 묶음이_null이면_비어_있음_오류이고_챕터가_null이면_챕터_필요_오류다()
        {
            BuildEdgeTestKit.AssertFails(() => HeroSkillDatabase.Build(null, _chapters, Exists, WorldMapTestSupport.Text), "영웅 스킬 데이터가 비어 있음");
            BuildEdgeTestKit.AssertFails(() => HeroSkillDatabase.Build(WorldMapTestSupport.ShippedHeroSkillData(), null, Exists, WorldMapTestSupport.Text),
                "영웅 스킬 검증에 챕터 데이터가 필요함");
        }

        [Test]
        public void 세_표가_모두_null이면_스킬_15종과_영웅_5명_누락을_한꺼번에_보고한다()
        {
            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(new HeroSkillDataSet()));

            foreach (EHeroSkill id in AllSkills())
            {
                StringAssert.Contains("HeroSkill.json 에 " + id + " 행이 없음", e.Message);
            }
            foreach (string heroId in HeroLoadouts.Keys)
            {
                StringAssert.Contains("영웅 " + heroId + " 의 행이 없음", e.Message);
            }
            Assert.AreEqual(20, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }

        [Test]
        public void 세_표가_빈_배열이어도_null과_같은_위반을_보고한다()
        {
            HeroSkillDataSet data = new HeroSkillDataSet { Skills = new HeroSkillRow[0], Params = new HeroSkill_ParamRow[0], Phases = new HeroArchetype_SkillRow[0] };

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            Assert.AreEqual(20, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }

        //# --- HeroSkill ---

        [TestCase("Unknown")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("0")]
        [TestCase("dashstrike")]
        public void 스킬_id가_EHeroSkill_값명이_아니면_위반이고_원래_스킬은_누락으로_보고한다(string skillId)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            data.Skills[0].skillId = skillId;

            AssertFails(data, "HeroSkill.json skillId '" + skillId + "' 가 EHeroSkill 에 없음", "HeroSkill.json 에 DashStrike 행이 없음");
        }

        [Test]
        public void 스킬_행이_null이면_위반으로_보고하고_예외로_새지_않는다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            data.Skills[3] = null;

            AssertFails(data, "HeroSkill.json skillId '' 가 EHeroSkill 에 없음", "HeroSkill.json 에 PiercingShot 행이 없음");
        }

        [Test]
        public void 스킬_id가_중복이면_오류다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            data.Skills[1].skillId = "DashStrike";

            AssertFails(data, "HeroSkill.json skillId DashStrike 중복", "HeroSkill.json 에 AoeNova 행이 없음");
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(99999)]
        public void 스킬_nameKey가_0_음수_없는_id면_위반이다(int nameKey)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            data.Skills[2].nameKey = nameKey;
            Func<int, bool> exists = id => id != 99999 && Exists(id);

            BuildEdgeTestKit.AssertFails(() => Build(data, exists), "HeroSkill[OrbitingBlade].nameKey " + nameKey + " 가 Strings_Ko 에 없음");
        }

        [Test]
        public void 스킬_표시_이름이_조회되지_않으면_빈_문자열로_조립한다()
        {
            HeroSkillDatabase db = HeroSkillDatabase.Build(WorldMapTestSupport.ShippedHeroSkillData(), _chapters, id => true, id => null);

            Assert.AreEqual(string.Empty, db.GetSkill(EHeroSkill.DashStrike).DisplayName);
        }

        [Test]
        public void GetSkill은_없는_값에_null을_돌려준다()
        {
            Assert.IsNull(_shipped.GetSkill((EHeroSkill)999));
            Assert.IsNull(_shipped.GetSkill((EHeroSkill)(-1)));
        }

        //# --- HeroSkill_Param ---

        [TestCaseSource(nameof(AllSkills))]
        public void 필수_수치_하나가_빠지면_그_키만_위반이다(EHeroSkill id)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            string missing = RequiredKeys[id][0];
            RemoveParam(data, id, missing);

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            StringAssert.Contains("HeroSkill_Param.json 에 " + id + "/" + missing + " 가 없음", e.Message);
            Assert.AreEqual(1, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }

        [TestCaseSource(nameof(AllSkills))]
        public void 필수가_아닌_수치가_붙으면_그_키만_위반이다(EHeroSkill id)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            AddParam(data, id.ToString(), "bogus", 1f);

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            StringAssert.Contains("HeroSkill_Param.json " + id + "/bogus 는 " + id + " 의 수치가 아님", e.Message);
            Assert.AreEqual(1, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }

        [TestCaseSource(nameof(AllSkills))]
        public void 스킬의_수치가_전부_없으면_필수_키_개수만큼_위반이다(EHeroSkill id)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            foreach (string key in RequiredKeys[id])
            {
                RemoveParam(data, id, key);
            }

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            Assert.AreEqual(RequiredKeys[id].Length, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }

        //# 정수 키 집합의 독립 기준 — 스킬 클래스가 int 로 읽는 프로퍼티(int)가 곧 정수 키다. 검증의 IntParams 표가 이와 어긋나면 여기서 잡힌다.
        [TestCaseSource(nameof(AllSkills))]
        public void 클래스가_int로_읽는_키는_소수_값이_위반이고_float_키는_소수가_통과한다(EHeroSkill id)
        {
            HeroSkillData skill = HeroSkillData.Create(_shipped.GetSkill(id));
            foreach (string key in RequiredKeys[id])
            {
                System.Reflection.PropertyInfo prop = skill.GetType().GetProperty(Pascal(key));
                Assert.IsNotNull(prop, id + "." + Pascal(key));
                HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
                FindParam(data, id, key).value += 0.5f;

                if (prop.PropertyType == typeof(int))
                {
                    ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));
                    StringAssert.Contains("HeroSkill_Param[" + id + "/" + key + "]", e.Message, key);
                    StringAssert.Contains("는 정수여야 함", e.Message, key);
                    continue;
                }
                Assert.DoesNotThrow(() => Build(data), id + "/" + key);
            }
        }

        [Test]
        public void 수치_행의_복합키가_중복이면_오류다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            AddParam(data, "AoeNova", "radius", 9f);

            AssertFails(data, "복합키 중복: AoeNova/radius");
        }

        [Test]
        public void 같은_paramKey라도_다른_스킬이면_중복이_아니다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();

            Assert.IsNotNull(FindParam(data, EHeroSkill.DashStrike, "damage"));
            Assert.IsNotNull(FindParam(data, EHeroSkill.AoeNova, "damage"));
            Assert.DoesNotThrow(() => Build(data));
        }

        [TestCase("Unknown")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("0")]
        public void 수치_행의_skillId가_없는_스킬이면_위반이다(string skillId)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            AddParam(data, skillId, "damage", 1f);

            AssertFails(data, "HeroSkill_Param.json skillId '" + skillId + "' 가 HeroSkill.json 에 없음");
        }

        [Test]
        public void 수치_행이_null이면_위반으로_보고하고_예외로_새지_않는다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            data.Params[0] = null;

            AssertFails(data, "HeroSkill_Param.json skillId '' 가 HeroSkill.json 에 없음", "HeroSkill_Param.json 에 DashStrike/damage 가 없음");
        }

        [TestCase("")]
        [TestCase(null)]
        public void 수치_행의_paramKey가_비어_있으면_위반이다(string paramKey)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            AddParam(data, "AoeNova", paramKey, 1f);

            AssertFails(data, "HeroSkill_Param.json AoeNova 의 paramKey 가 비어 있음");
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.NaN)]
        public void 수치_값이_0_이하이거나_NaN이면_위반이다(float value)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            FindParam(data, EHeroSkill.AoeNova, "cooldown").value = value;

            AssertFails(data, "HeroSkill_Param[AoeNova/cooldown].value");
        }

        [Test]
        public void 수치_값이_0에_가까운_양수면_통과한다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            FindParam(data, EHeroSkill.AoeNova, "cooldown").value = 0.0001f;

            Assert.AreEqual(0.0001f, Build(data).GetSkill(EHeroSkill.AoeNova).Params.Get("cooldown"), 0f);
        }

        [Test]
        public void 정수_키에_정수_값이면_통과하고_소수_값이면_위반이다()
        {
            HeroSkillDataSet whole = WorldMapTestSupport.ShippedHeroSkillData();
            FindParam(whole, EHeroSkill.AoeNova, "damage").value = 101f;
            HeroSkillDataSet fraction = WorldMapTestSupport.ShippedHeroSkillData();
            FindParam(fraction, EHeroSkill.AoeNova, "damage").value = 100.4f;

            Assert.AreEqual(101, Build(whole).GetSkill(EHeroSkill.AoeNova).Params.GetInt("damage"));
            AssertFails(fraction, "HeroSkill_Param[AoeNova/damage].value 100.4 는 정수여야 함");
        }

        //# --- HeroArchetype_Skill ---

        [TestCase("Ghost")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("knight")]
        public void 구간_행의_heroId가_영웅이_아니면_위반이다(string heroId)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            data.Phases[0].heroId = heroId;

            AssertFails(data, "HeroArchetype_Skill.json heroId '" + heroId + "' 가 HeroArchetype.json 에 없음");
        }

        [Test]
        public void 구간_행이_null이면_위반으로_보고하고_예외로_새지_않는다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            data.Phases[3] = null;

            AssertFails(data, "HeroArchetype_Skill.json heroId '' 가 HeroArchetype.json 에 없음");
        }

        [TestCase("Bogus")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("1")]
        public void 구간_행의_skillId가_스킬이_아니면_위반이다(string skillId)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            FindPhase(data, "Knight", 2).skillId = skillId;

            AssertFails(data, "HeroArchetype_Skill[Knight/2].skillId '" + skillId + "' 가 HeroSkill.json 에 없음");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void 구간_번호가_1_미만이면_위반이다(int phaseIndex)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            FindPhase(data, "Knight", 1).phaseIndex = phaseIndex;

            AssertFails(data, "HeroArchetype_Skill[Knight/" + phaseIndex + "] phaseIndex 는 1 이상이어야 함");
        }

        [Test]
        public void 같은_영웅_구간_번호가_두_번이면_복합키_중복이다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            List<HeroArchetype_SkillRow> rows = new List<HeroArchetype_SkillRow>(data.Phases);
            rows.Add(new HeroArchetype_SkillRow { heroId = "Knight", phaseIndex = 2, hpFraction = 0.5f, skillId = "DashStrike" });
            data.Phases = rows.ToArray();

            AssertFails(data, "복합키 중복: Knight/2");
        }

        [Test]
        public void 같은_구간_번호라도_영웅이_다르면_중복이_아니다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();

            Assert.AreEqual(FindPhase(data, "Knight", 1).phaseIndex, FindPhase(data, "Archer", 1).phaseIndex);
            Assert.DoesNotThrow(() => Build(data));
        }

        [Test]
        public void 구간_번호가_2부터_시작하면_1부터_연속_위반이다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            for (int i = 3; i >= 1; i--)
            {
                FindPhase(data, "Knight", i).phaseIndex = i + 1;
            }

            AssertFails(data, "HeroArchetype_Skill[Knight] phaseIndex 는 1부터 연속이어야 함 (기대 1, 실제 2)");
        }

        [Test]
        public void 구간_번호가_중간에_끊기면_기대와_실제_번호를_보고한다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            FindPhase(data, "Archer", 3).phaseIndex = 5;

            AssertFails(data, "HeroArchetype_Skill[Archer] phaseIndex 는 1부터 연속이어야 함 (기대 3, 실제 5)");
        }

        [TestCase(0f)]
        [TestCase(1f)]
        [TestCase(1.0001f)]
        [TestCase(-0.1f)]
        [TestCase(2f)]
        public void 해금_HP_비율은_0_초과_1_미만이어야_한다(float fraction)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            FindPhase(data, "Knight", 1).hpFraction = fraction;

            AssertFails(data, "HeroArchetype_Skill[Knight/1].hpFraction");
        }

        [Test]
        public void 해금_HP_비율_경계값은_통과한다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            FindPhase(data, "Knight", 1).hpFraction = 0.9999f;
            FindPhase(data, "Knight", 3).hpFraction = 0.0001f;

            HeroSkillDatabase db = Build(data);

            Assert.AreEqual(0.9999f, db.GetPhases("Knight")[0].HpFraction, 0f);
            Assert.AreEqual(0.0001f, db.GetPhases("Knight")[2].HpFraction, 0f);
        }

        [TestCase(0.85f)]
        [TestCase(0.9f)]
        public void 다음_구간_비율이_이전과_같거나_크면_위반이다(float fraction)
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            FindPhase(data, "Paladin", 2).hpFraction = fraction;

            AssertFails(data, "HeroArchetype_Skill[Paladin/2].hpFraction " + fraction + " 는 이전 구간보다 작아야 함");
        }

        [Test]
        public void 구간이_하나뿐인_영웅도_통과한다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            RemovePhases(data, row => row.heroId == "Knight" && row.phaseIndex > 1);

            Assert.AreEqual(1, Build(data).GetPhases("Knight").Count);
        }

        [Test]
        public void 구간이_4개인_영웅도_통과하고_번호_순서를_지킨다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            List<HeroArchetype_SkillRow> rows = new List<HeroArchetype_SkillRow>(data.Phases);
            rows.Add(new HeroArchetype_SkillRow { heroId = "Knight", phaseIndex = 4, hpFraction = 0.25f, skillId = "Sanctuary" });
            data.Phases = rows.ToArray();

            IReadOnlyList<HeroSkillPhaseEntry> phases = Build(data).GetPhases("Knight");

            Assert.AreEqual(4, phases.Count);
            Assert.AreEqual(EHeroSkill.Sanctuary, phases[3].Skill);
            Assert.AreEqual(0.25f, phases[3].HpFraction, 0f);
        }

        [Test]
        public void 구간_행_입력_순서가_뒤섞여도_번호_순_내림차순_비율로_조회된다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            Array.Reverse(data.Phases);

            HeroSkillDatabase db = Build(data);

            foreach (KeyValuePair<string, EHeroSkill[]> pair in HeroLoadouts)
            {
                IReadOnlyList<HeroSkillPhaseEntry> phases = db.GetPhases(pair.Key);
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(i + 1, phases[i].PhaseIndex, pair.Key);
                    Assert.AreEqual(pair.Value[i], phases[i].Skill, pair.Key);
                }
            }
        }

        //# --- GetPhases ---

        [Test]
        public void GetPhases는_없는_영웅_null_빈_문자열_대소문자_다름에_빈_목록을_돌려준다()
        {
            Assert.IsNotNull(_shipped.GetPhases("Ghost"));
            Assert.AreEqual(0, _shipped.GetPhases("Ghost").Count);
            Assert.AreEqual(0, _shipped.GetPhases(null).Count);
            Assert.AreEqual(0, _shipped.GetPhases(string.Empty).Count);
            Assert.AreEqual(0, _shipped.GetPhases("knight").Count);
        }

        //# --- GateFractionsOf (§6.3 이중 진실 제거) ---

        [Test]
        public void GateFractionsOf는_null_묶음_null_구간_배열에_빈_배열이다()
        {
            Assert.AreEqual(0, HeroSkillDatabase.GateFractionsOf(null).Length);
            Assert.AreEqual(0, HeroSkillDatabase.GateFractionsOf(new HeroSkillDataSet()).Length);
            Assert.AreEqual(0, HeroSkillDatabase.GateFractionsOf(new HeroSkillDataSet { Phases = new HeroArchetype_SkillRow[0] }).Length);
        }

        [Test]
        public void GateFractionsOf는_같은_비율을_한_번만_처음_나온_순서로_돌려준다()
        {
            HeroSkillDataSet data = new HeroSkillDataSet
            {
                Phases = new[]
                {
                    new HeroArchetype_SkillRow { heroId = "A", phaseIndex = 1, hpFraction = 0.7f },
                    new HeroArchetype_SkillRow { heroId = "A", phaseIndex = 2, hpFraction = 0.3f },
                    null,
                    new HeroArchetype_SkillRow { heroId = "B", phaseIndex = 1, hpFraction = 0.7f },
                    new HeroArchetype_SkillRow { heroId = "B", phaseIndex = 2, hpFraction = 0.5f },
                    new HeroArchetype_SkillRow { heroId = "B", phaseIndex = 3, hpFraction = 0.3f },
                },
            };

            CollectionAssert.AreEqual(new[] { 0.7f, 0.3f, 0.5f }, HeroSkillDatabase.GateFractionsOf(data));
        }

        [Test]
        public void GateFractionsOf는_검증_전_원본_행에서도_구해지고_잘못된_영웅_행도_센다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            data.Phases[0].heroId = "Ghost";
            data.Phases[1].hpFraction = 0.55f;

            float[] gates = HeroSkillDatabase.GateFractionsOf(data);

            CollectionAssert.AreEquivalent(new[] { 0.85f, 0.55f, 0.45f, 0.65f }, gates);
        }

        [Test]
        public void GateFractionsOf_출하_값은_세_종류_0_85_0_65_0_45다()
        {
            float[] gates = HeroSkillDatabase.GateFractionsOf(WorldMapTestSupport.ShippedHeroSkillData());

            CollectionAssert.AreEquivalent(PhaseFractions, gates);
            Assert.AreEqual(3, gates.Length, "영웅 5명 x 구간 3 = 15행이 3종으로 중복 제거");
        }

        //# --- 위반 수집 ---

        [Test]
        public void 세_표의_위반이_섞여도_메시지에_전부_담긴다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            data.Skills[0].nameKey = 0;
            RemoveParam(data, EHeroSkill.AoeNova, "radius");
            AddParam(data, "Sanctuary", "bogus", 1f);
            FindPhase(data, "Knight", 2).hpFraction = 0.95f;
            FindPhase(data, "Archer", 1).skillId = "Bogus";
            RemovePhases(data, row => row.heroId == "Paladin");

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            StringAssert.Contains("HeroSkill[DashStrike].nameKey 0", e.Message);
            StringAssert.Contains("AoeNova/radius 가 없음", e.Message);
            StringAssert.Contains("Sanctuary/bogus", e.Message);
            StringAssert.Contains("HeroArchetype_Skill[Knight/2].hpFraction", e.Message);
            StringAssert.Contains("HeroArchetype_Skill[Archer/1].skillId 'Bogus'", e.Message);
            StringAssert.Contains("영웅 Paladin 의 행이 없음", e.Message);
        }
    }

    //# HeroSkillParams — 수치 묶음 조회 (§3.9: 정수 의미 값은 float 로 저장하고 읽을 때 반올림).
    public class HeroSkillParamsTests
    {
        private static HeroSkillParams Params(params (string key, float value)[] pairs)
        {
            Dictionary<string, float> values = new Dictionary<string, float>();
            foreach ((string key, float value) pair in pairs)
            {
                values[pair.key] = pair.value;
            }
            return new HeroSkillParams(values);
        }

        [Test]
        public void Get은_저장된_값을_그대로_돌려주고_없는_키는_예외다()
        {
            HeroSkillParams p = Params(("cooldown", 3.25f));

            Assert.AreEqual(3.25f, p.Get("cooldown"), 0f);
            Assert.Throws<KeyNotFoundException>(() => p.Get("missing"));
            Assert.Throws<KeyNotFoundException>(() => p.GetInt("missing"));
        }

        [TestCase(2.4f, 2)]
        [TestCase(2.6f, 3)]
        [TestCase(3f, 3)]
        [TestCase(79.6f, 80)]
        [TestCase(79.4f, 79)]
        [TestCase(0.4f, 0)]
        [TestCase(0.6f, 1)]
        public void GetInt는_가까운_정수로_반올림한다(float value, int expected)
        {
            Assert.AreEqual(expected, Params(("damage", value)).GetInt("damage"));
        }

        [Test]
        public void Values는_조립_키_수만큼_읽기_전용으로_노출한다()
        {
            HeroSkillParams p = Params(("a", 1f), ("b", 2f));

            Assert.AreEqual(2, p.Values.Count);
            Assert.AreEqual(2f, p.Values["b"], 0f);
        }
    }
}
