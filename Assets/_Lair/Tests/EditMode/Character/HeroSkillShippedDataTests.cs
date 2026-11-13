using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.EditMode
{
    //# 출하 데이터 회귀 (so-to-json-migration §10) — 영웅 스킬 JSON 3종이 로드 검증(SJ3~SJ5)을 통과하고 에셋 키 규약(SJ6)을 지킨다.
    public class HeroSkillShippedDataTests
    {
        private const string GroupDir = "Assets/AddressableAssetsData/AssetGroups";
        private static readonly EData[] Keys = { EData.HeroSkill, EData.HeroSkill_Param, EData.HeroArchetype_Skill };

        [Test]
        public void 출하_영웅_스킬_JSON은_검증을_통과하고_행_수가_기획서와_같다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            Assert.AreEqual(15, data.Skills.Length, "HeroSkill");
            Assert.AreEqual(97, data.Params.Length, "HeroSkill_Param");
            Assert.AreEqual(15, data.Phases.Length, "HeroArchetype_Skill");

            HeroSkillDatabase db = WorldMapTestSupport.BuildHeroSkills(data);

            int params97 = 0;
            foreach (EHeroSkill id in System.Enum.GetValues(typeof(EHeroSkill)))
            {
                HeroSkillEntry entry = db.GetSkill(id);
                Assert.IsNotNull(entry, id.ToString());
                params97 += entry.Params.Values.Count;
            }
            Assert.AreEqual(97, params97);
            foreach (string heroId in WorldMapTestSupport.FullDb().HeroIds)
            {
                Assert.AreEqual(3, db.GetPhases(heroId).Count, heroId);
            }
        }

        [Test]
        public void Strings_Ko_1617에서_1631은_15개가_연속_존재하고_비어있지_않다()
        {
            for (int id = 1617; id <= 1631; id++)
            {
                Assert.IsTrue(WorldMapTestSupport.Strings().ContainsKey(id), $"id {id} 없음");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"id {id} 비어 있음");
            }
        }

        //# 구간 HP 비율의 종류는 보스 규칙 트리거가 피해야 하는 스킬 게이트의 이전 하드코딩 값(0.85·0.65·0.45)과 같다 —
        //# 그 하드코딩 사본을 HeroArchetype_Skill 집합(GateFractionsOf)으로 대체해도 챕터 검증 결과가 달라지지 않는다는 근거.
        [Test]
        public void 구간_HP_비율의_종류는_스킬_게이트_세_값과_같다()
        {
            HeroSkillDatabase db = WorldMapTestSupport.ShippedHeroSkills();
            SortedSet<float> distinct = new SortedSet<float>();
            foreach (string heroId in WorldMapTestSupport.FullDb().HeroIds)
            {
                foreach (HeroSkillPhaseEntry phase in db.GetPhases(heroId))
                {
                    distinct.Add(phase.HpFraction);
                }
            }

            float[] former = { 0.85f, 0.65f, 0.45f };
            CollectionAssert.AreEquivalent(former, distinct);
            CollectionAssert.AreEquivalent(former, HeroSkillDatabase.GateFractionsOf(WorldMapTestSupport.ShippedHeroSkillData()));
        }

        //# JsonUtility 가 오타·누락 필드를 0 으로 채우는 경우(nameKey·hpFraction)와 수치 키 누락·초과·정수 위반을 한 번에 모아 던진다.
        [Test]
        public void 수치_누락_초과_정수_위반과_0_채움을_모아_던진다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            List<HeroSkill_ParamRow> rows = new List<HeroSkill_ParamRow>(data.Params);
            rows.RemoveAll(row => row.skillId == "DashStrike" && row.paramKey == "damage");
            rows.Add(new HeroSkill_ParamRow { skillId = "AoeNova", paramKey = "bogus", value = 1f });
            rows.Find(row => row.skillId == "OrbitingBlade" && row.paramKey == "bladeCount").value = 2.5f;
            data.Params = rows.ToArray();
            data.Skills[0].nameKey = 0;
            data.Phases[0].hpFraction = 0f;

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildHeroSkills(data));

            StringAssert.Contains("DashStrike/damage 가 없음", e.Message);
            StringAssert.Contains("AoeNova/bogus 는 AoeNova 의 수치가 아님", e.Message);
            StringAssert.Contains("OrbitingBlade/bladeCount", e.Message);
            StringAssert.Contains("HeroSkill[DashStrike].nameKey 0", e.Message);
            StringAssert.Contains("Knight/1].hpFraction", e.Message);
        }

        [Test]
        public void 영웅_구간이_끊기거나_영웅_행이_없거나_없는_영웅이면_위반이다()
        {
            HeroSkillDataSet data = WorldMapTestSupport.ShippedHeroSkillData();
            List<HeroArchetype_SkillRow> rows = new List<HeroArchetype_SkillRow>(data.Phases);
            rows.RemoveAll(row => row.heroId == "Paladin" && row.phaseIndex == 2);
            rows.RemoveAll(row => row.heroId == "Berserker");
            rows.Add(new HeroArchetype_SkillRow { heroId = "Ghost", phaseIndex = 1, hpFraction = 0.5f, skillId = "DashStrike" });
            data.Phases = rows.ToArray();

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildHeroSkills(data));

            StringAssert.Contains("HeroArchetype_Skill[Paladin] phaseIndex 는 1부터 연속이어야 함", e.Message);
            StringAssert.Contains("영웅 Berserker 의 행이 없음", e.Message);
            StringAssert.Contains("heroId 'Ghost' 가 HeroArchetype.json 에 없음", e.Message);
        }

        [Test]
        public void 영웅_스킬_JSON_3종은_EData_값명_파일로_Addressables에_Resource_라벨로_한_번만_등록돼_있다()
        {
            string groups = string.Empty;
            foreach (string path in Directory.GetFiles(GroupDir, "*.asset"))
            {
                groups += File.ReadAllText(path).Replace("\r\n", "\n") + "\n";
            }

            foreach (EData key in Keys)
            {
                string jsonPath = WorldMapTestSupport.JsonDir + key + ".json";
                Assert.IsTrue(File.Exists(jsonPath), jsonPath);
                string entry = "  - m_GUID: " + AssetDatabase.AssetPathToGUID(jsonPath) + "\n    m_Address: " + key
                    + "\n    m_ReadOnly: 0\n    m_SerializedLabels:\n    - Resource\n";
                StringAssert.Contains(entry, groups, key.ToString());
                Assert.AreEqual(1, Regex.Matches(groups, "m_Address: " + key + "\n").Count, key + " 주소 중복");
            }
        }
    }
}
