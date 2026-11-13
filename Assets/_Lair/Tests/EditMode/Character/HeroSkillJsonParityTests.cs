using System.Collections.Generic;
using System.Reflection;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# SJ1 일회용 패리티 (so-to-json-migration §10) — 스킬 SO 15개·로드아웃 SO 5개(이전 전 값)와 JSON 조립 결과를 전 필드·순서까지 비교한다.
    //# 커밋 ② 에서 SO 에셋과 함께 이 파일을 삭제한다. 수치 키는 SO 의 [SerializeField] 필드명(앞 '_' 제외)이다.
    public class HeroSkillJsonParityTests
    {
        private const string SkillDir = "Assets/_Lair/Art/Skills/";

        private HeroSkillDatabase _json;

        [SetUp]
        public void SetUp()
        {
            _json = WorldMapTestSupport.ShippedHeroSkills();
        }

        private static HeroSkillData LoadSkill(EHeroSkill id)
        {
            HeroSkillData data = AssetDatabase.LoadAssetAtPath<HeroSkillData>(SkillDir + "HeroSkill_" + id + ".asset");
            Assert.IsNotNull(data, id.ToString());
            return data;
        }

        //# 서브클래스가 선언한 [SerializeField] 수치 필드 — 베이스의 _displayName 은 제외된다.
        private static List<FieldInfo> TuningFields(HeroSkillData skill)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            foreach (FieldInfo field in skill.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.GetCustomAttribute<SerializeField>() != null)
                {
                    result.Add(field);
                }
            }
            return result;
        }

        [Test]
        public void 스킬_15개는_클래스_이름_표시_이름이_같다()
        {
            foreach (EHeroSkill id in System.Enum.GetValues(typeof(EHeroSkill)))
            {
                HeroSkillData skill = LoadSkill(id);
                Assert.AreEqual(id + "SkillData", skill.GetType().Name, id + " 클래스");
                Assert.AreEqual(skill.DisplayName, _json.GetSkill(id).DisplayName, id + " DisplayName");
            }
        }

        //# 정수 필드는 반올림 값(GetInt), 실수 필드는 오차 0 으로 비교한다. 필드 집합이 키 집합과 양방향으로 같아야 누락·초과가 없다.
        [Test]
        public void 수치_97개는_필드_집합과_값이_모두_같다()
        {
            int total = 0;
            foreach (EHeroSkill id in System.Enum.GetValues(typeof(EHeroSkill)))
            {
                HeroSkillData skill = LoadSkill(id);
                HeroSkillParams actual = _json.GetSkill(id).Params;
                List<FieldInfo> fields = TuningFields(skill);
                List<string> keys = new List<string>();
                foreach (FieldInfo field in fields)
                {
                    keys.Add(field.Name.TrimStart('_'));
                }
                CollectionAssert.AreEquivalent(keys, new List<string>(actual.Values.Keys), id + " paramKey 집합");

                foreach (FieldInfo field in fields)
                {
                    string key = field.Name.TrimStart('_');
                    string at = id + "/" + key;
                    if (field.FieldType == typeof(int))
                    {
                        Assert.AreEqual((int)field.GetValue(skill), actual.GetInt(key), at);
                    }
                    else
                    {
                        Assert.AreEqual((float)field.GetValue(skill), actual.Get(key), at);
                    }
                    total++;
                }
            }
            Assert.AreEqual(97, total, "수치 총 개수");
        }

        //# 영웅 → 로드아웃 매핑은 BattleController.LoadoutKeys(코드 표)와 같아야 HeroArchetype_Skill.heroId 가 그 표를 대체할 수 있다.
        [Test]
        public void 로드아웃_5개는_구간_수_HP_비율_스킬_순서가_같고_영웅_매핑이_LoadoutKeys_와_일치한다()
        {
            FieldInfo keysField = typeof(BattleController).GetField("LoadoutKeys", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(keysField, "BattleController.LoadoutKeys");
            Dictionary<EHero, EData> keys = (Dictionary<EHero, EData>)keysField.GetValue(null);
            Assert.AreEqual(5, keys.Count);

            List<string> heroNames = new List<string>();
            foreach (KeyValuePair<EHero, EData> pair in keys)
            {
                heroNames.Add(pair.Key.ToString());
                HeroSkillLoadout loadout = AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>(SkillDir + pair.Value + ".asset");
                Assert.IsNotNull(loadout, pair.Value.ToString());
                IReadOnlyList<HeroSkillPhaseEntry> phases = _json.GetPhases(pair.Key.ToString());

                Assert.AreEqual(loadout.Phases.Count, phases.Count, pair.Key + " 구간 수");
                for (int i = 0; i < phases.Count; i++)
                {
                    string at = pair.Key + "[" + i + "]";
                    Assert.AreEqual(i + 1, phases[i].PhaseIndex, at + " phaseIndex");
                    Assert.AreEqual(loadout.Phases[i].HpFraction, phases[i].HpFraction, at + " hpFraction");
                    Assert.AreEqual("HeroSkill_" + phases[i].Skill, loadout.Phases[i].Skill.name, at + " skill");
                }
            }
            CollectionAssert.AreEquivalent(WorldMapTestSupport.FullDb().HeroIds, heroNames, "영웅 5명 = HeroArchetype 5행");
        }
    }
}
