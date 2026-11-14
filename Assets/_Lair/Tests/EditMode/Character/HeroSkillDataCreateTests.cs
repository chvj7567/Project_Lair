using System;
using System.Collections.Generic;
using System.Reflection;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# HeroSkillData.Create — EHeroSkill 15값 전부에 대해 구체 타입과 JSON → 프로퍼티 수치 전달 (so-to-json-migration §7.3).
    public class HeroSkillDataCreateTests
    {
        //# 스킬별 CastPriority — 같은 프레임에 여러 스킬 조건이 참일 때의 우선순위(전환 전과 동일해야 함, 클수록 먼저).
        private static readonly Dictionary<EHeroSkill, int> CastPriorities = new Dictionary<EHeroSkill, int>
        {
            { EHeroSkill.ShadowStrike, 1 },
            { EHeroSkill.SmokeBomb, 2 },
            { EHeroSkill.ChainPull, 2 },
            { EHeroSkill.BloodFrenzy, 1 },
            { EHeroSkill.PurifyingLight, 2 },
            { EHeroSkill.Sanctuary, 1 },
        };

        private HeroSkillDatabase _shipped;

        [OneTimeSetUp]
        public void 출하_스킬_DB를_한_번_조립한다()
        {
            _shipped = WorldMapTestSupport.ShippedHeroSkills();
        }

        private static IEnumerable<EHeroSkill> AllSkills()
        {
            foreach (EHeroSkill id in Enum.GetValues(typeof(EHeroSkill)))
            {
                yield return id;
            }
        }

        private static string Pascal(string key) => char.ToUpperInvariant(key[0]) + key.Substring(1);

        private static PropertyInfo[] ValueProperties(HeroSkillData skill)
        {
            List<PropertyInfo> result = new List<PropertyInfo>();
            foreach (PropertyInfo prop in skill.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (prop.Name != "CastPriority")
                {
                    result.Add(prop);
                }
            }
            return result.ToArray();
        }

        //# 키마다 값이 모두 다른 합성 수치 — 출하 값이 우연히 같아 "필드 뒤바뀜" 이 가려지는 것을 막는다. 값은 정수라 int·float 프로퍼티 모두 그대로 읽힌다.
        private static HeroSkillEntry Synthetic(EHeroSkill id, out Dictionary<string, float> values)
        {
            values = new Dictionary<string, float>();
            string[] keys = HeroSkillDatabaseEdgeTests.RequiredKeys[id];
            for (int i = 0; i < keys.Length; i++)
            {
                values[keys[i]] = (i + 1) * 7f;
            }
            return new HeroSkillEntry { Id = id, DisplayName = "이름_" + id, Params = new HeroSkillParams(values) };
        }

        private static float ReadAsFloat(PropertyInfo prop, object target)
        {
            return Convert.ToSingle(prop.GetValue(target));
        }

        [TestCaseSource(nameof(AllSkills))]
        public void 구체_타입과_표시_이름이_스킬_id에서_정해진다(EHeroSkill id)
        {
            HeroSkillEntry entry = _shipped.GetSkill(id);

            HeroSkillData skill = HeroSkillData.Create(entry);

            Assert.AreEqual(id + "SkillData", skill.GetType().Name);
            Assert.AreEqual(entry.DisplayName, skill.DisplayName);
        }

        [TestCaseSource(nameof(AllSkills))]
        public void 출하_수치_키마다_같은_이름_프로퍼티가_JSON_값을_가진다(EHeroSkill id)
        {
            HeroSkillEntry entry = _shipped.GetSkill(id);
            HeroSkillData skill = HeroSkillData.Create(entry);

            foreach (KeyValuePair<string, float> pair in entry.Params.Values)
            {
                PropertyInfo prop = skill.GetType().GetProperty(Pascal(pair.Key));

                Assert.IsNotNull(prop, id + "." + Pascal(pair.Key) + " 프로퍼티 없음");
                if (prop.PropertyType == typeof(int))
                {
                    Assert.AreEqual(entry.Params.GetInt(pair.Key), (int)prop.GetValue(skill), id + "." + pair.Key);
                    continue;
                }
                Assert.AreEqual(pair.Value, ReadAsFloat(prop, skill), 0f, id + "." + pair.Key);
            }
        }

        //# 프로퍼티 수 = 수치 키 수 — JSON 에 있는데 클래스가 읽지 않는 값, 클래스가 있는데 JSON 이 채우지 않는 값이 없다.
        [TestCaseSource(nameof(AllSkills))]
        public void 수치_프로퍼티_개수가_필수_키_개수와_같다(EHeroSkill id)
        {
            HeroSkillData skill = HeroSkillData.Create(_shipped.GetSkill(id));

            Assert.AreEqual(HeroSkillDatabaseEdgeTests.RequiredKeys[id].Length, ValueProperties(skill).Length, id.ToString());
        }

        [TestCaseSource(nameof(AllSkills))]
        public void 키마다_다른_값을_넣어도_각_프로퍼티가_자기_키_값을_읽는다(EHeroSkill id)
        {
            HeroSkillEntry entry = Synthetic(id, out Dictionary<string, float> values);

            HeroSkillData skill = HeroSkillData.Create(entry);

            foreach (KeyValuePair<string, float> pair in values)
            {
                PropertyInfo prop = skill.GetType().GetProperty(Pascal(pair.Key));

                Assert.IsNotNull(prop, id + "." + Pascal(pair.Key));
                Assert.AreEqual(pair.Value, ReadAsFloat(prop, skill), 0f, id + "." + pair.Key);
            }
            Assert.AreEqual("이름_" + id, skill.DisplayName);
        }

        [TestCaseSource(nameof(AllSkills))]
        public void 런타임은_스킬_id_이름의_Runtime_타입이고_호출마다_새_인스턴스다(EHeroSkill id)
        {
            HeroSkillData skill = HeroSkillData.Create(_shipped.GetSkill(id));

            IHeroSkillRuntime first = skill.CreateRuntime();
            IHeroSkillRuntime second = skill.CreateRuntime();

            Assert.IsNotNull(first);
            Assert.AreEqual(id + "Runtime", first.GetType().Name);
            Assert.AreNotSame(first, second, "런타임은 가변 상태를 가지므로 공유되면 안 된다");
        }

        [TestCaseSource(nameof(AllSkills))]
        public void Create는_호출마다_새_스킬_인스턴스를_만든다(EHeroSkill id)
        {
            HeroSkillEntry entry = _shipped.GetSkill(id);

            Assert.AreNotSame(HeroSkillData.Create(entry), HeroSkillData.Create(entry));
        }

        [TestCaseSource(nameof(AllSkills))]
        public void CastPriority는_전환_전_값과_같다(EHeroSkill id)
        {
            HeroSkillData skill = HeroSkillData.Create(_shipped.GetSkill(id));

            int expected = CastPriorities.TryGetValue(id, out int priority) ? priority : 0;
            Assert.AreEqual(expected, skill.CastPriority, id.ToString());
        }

        //# 정수 의미 값(damage 등)은 float 로 저장돼 읽을 때 반올림된다 — 합성 값으로 반올림 방향을 고정.
        [TestCase(79.6f, 80)]
        [TestCase(79.4f, 79)]
        [TestCase(100f, 100)]
        public void 정수_프로퍼티는_float_값을_반올림해_읽는다(float damage, int expected)
        {
            Dictionary<string, float> values = new Dictionary<string, float>
            {
                { "damage", damage }, { "cooldown", 5f }, { "radius", 3.5f }, { "knockbackStrength", 3f },
            };
            HeroSkillEntry entry = new HeroSkillEntry { Id = EHeroSkill.AoeNova, DisplayName = "n", Params = new HeroSkillParams(values) };

            AoeNovaSkillData skill = (AoeNovaSkillData)HeroSkillData.Create(entry);

            Assert.AreEqual(expected, skill.Damage);
        }

        //# --- 지원하지 않는 입력 ---

        [TestCase(999)]
        [TestCase(-1)]
        [TestCase(15)]
        public void Enum_범위_밖_스킬은_ArgumentOutOfRangeException이다(int raw)
        {
            HeroSkillEntry entry = new HeroSkillEntry { Id = (EHeroSkill)raw, DisplayName = "x", Params = new HeroSkillParams(new Dictionary<string, float>()) };

            Assert.Throws<ArgumentOutOfRangeException>(() => HeroSkillData.Create(entry));
        }

        [Test]
        public void 필수_수치가_빠진_묶음은_생성자에서_KeyNotFoundException이다()
        {
            Dictionary<string, float> values = new Dictionary<string, float> { { "cooldown", 5f }, { "radius", 3.5f }, { "knockbackStrength", 3f } };
            HeroSkillEntry entry = new HeroSkillEntry { Id = EHeroSkill.AoeNova, DisplayName = "n", Params = new HeroSkillParams(values) };

            Assert.Throws<KeyNotFoundException>(() => HeroSkillData.Create(entry));
        }
    }
}
