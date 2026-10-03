using System;
using System.Collections.Generic;
using System.IO;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using Lair.EditorTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 궁수 데이터 일관성 — hero_skills.json ↔ 스킬 .asset ↔ 로드아웃 .asset, heroId 구형 JSON 호환, JsonSync DTO 왕복 (chapter-system §1.4·§5.4).
    //# 실제 프로젝트 파일은 수정하지 않는다(읽기/메모리 왕복만).
    public class ArcherDataConsistencyTests : ChapterStaticStateTestBase
    {
        private const string SkillDir = "Assets/_Lair/Art/Skills/";
        private const string JsonPath = "Assets/_Lair/Data/Json/hero_skills.json";

        private static JObject Root()
        {
            return JObject.Parse(File.ReadAllText(JsonPath));
        }

        private static JObject SkillJson(string fileName)
        {
            foreach (JToken token in (JArray)Root()["skills"])
            {
                if ((string)token["fileName"] == fileName)
                    return (JObject)token;
            }
            Assert.Fail($"hero_skills.json 에 {fileName} 없음");
            return null;
        }

        private static JsonSerializerSettings Settings()
        {
            JsonSerializerSettings settings = JsonSyncSettings.Build();
            settings.Converters.Add(new HeroSkillDataConverter());
            return settings;
        }

        private static void DestroySkills(HeroSkillsDto dto)
        {
            foreach (HeroSkillData skill in dto.Skills)
            {
                UnityEngine.Object.DestroyImmediate(skill);
            }
        }

        //# --- 스킬 asset ↔ JSON ---

        [Test]
        public void 관통_사격_asset은_JSON_수치와_일치한다()
        {
            PiercingShotSkillData asset = AssetDatabase.LoadAssetAtPath<PiercingShotSkillData>(SkillDir + "HeroSkill_PiercingShot.asset");
            JObject json = SkillJson("HeroSkill_PiercingShot");

            Assert.AreEqual("PiercingShotSkillData", (string)json["$type"]);
            Assert.AreEqual((int)json["damage"], asset.Damage);
            Assert.AreEqual((float)json["cooldown"], asset.Cooldown, 0.0001f);
            Assert.AreEqual((float)json["lineLength"], asset.LineLength, 0.0001f);
            Assert.AreEqual((float)json["lineHalfWidth"], asset.LineHalfWidth, 0.0001f);
            Assert.AreEqual((float)json["knockbackStrength"], asset.KnockbackStrength, 0.0001f);
            Assert.AreEqual((float)json["centroidRadius"], asset.CentroidRadius, 0.0001f);
            Assert.AreEqual((string)json["displayName"], asset.DisplayName);
        }

        [Test]
        public void 화살_소나기_asset은_JSON_수치와_일치한다()
        {
            ArrowVolleySkillData asset = AssetDatabase.LoadAssetAtPath<ArrowVolleySkillData>(SkillDir + "HeroSkill_ArrowVolley.asset");
            JObject json = SkillJson("HeroSkill_ArrowVolley");

            Assert.AreEqual("ArrowVolleySkillData", (string)json["$type"]);
            Assert.AreEqual((int)json["damagePerTick"], asset.DamagePerTick);
            Assert.AreEqual((int)json["tickCount"], asset.TickCount);
            Assert.AreEqual((float)json["tickIntervalSec"], asset.TickIntervalSec, 0.0001f);
            Assert.AreEqual((float)json["radius"], asset.Radius, 0.0001f);
            Assert.AreEqual((float)json["cooldown"], asset.Cooldown, 0.0001f);
            Assert.AreEqual((float)json["targetSearchRadius"], asset.TargetSearchRadius, 0.0001f);
            Assert.AreEqual((string)json["displayName"], asset.DisplayName);
        }

        [Test]
        public void 회피_도약_asset은_JSON_수치와_일치한다()
        {
            EvadeSkillData asset = AssetDatabase.LoadAssetAtPath<EvadeSkillData>(SkillDir + "HeroSkill_Evade.asset");
            JObject json = SkillJson("HeroSkill_Evade");

            Assert.AreEqual("EvadeSkillData", (string)json["$type"]);
            Assert.AreEqual((int)json["damage"], asset.Damage);
            Assert.AreEqual((float)json["cooldown"], asset.Cooldown, 0.0001f);
            Assert.AreEqual((float)json["leapDistance"], asset.LeapDistance, 0.0001f);
            Assert.AreEqual((float)json["leapDurationSec"], asset.LeapDurationSec, 0.0001f);
            Assert.AreEqual((float)json["blastRadius"], asset.BlastRadius, 0.0001f);
            Assert.AreEqual((float)json["knockbackStrength"], asset.KnockbackStrength, 0.0001f);
            Assert.AreEqual((int)json["triggerMonsterCount"], asset.TriggerMonsterCount);
            Assert.AreEqual((float)json["triggerRadius"], asset.TriggerRadius, 0.0001f);
            Assert.AreEqual((string)json["displayName"], asset.DisplayName);
        }

        [Test]
        public void 궁수_스킬_asset_수치는_기획서_5_4_표와_같다()
        {
            PiercingShotSkillData pierce = AssetDatabase.LoadAssetAtPath<PiercingShotSkillData>(SkillDir + "HeroSkill_PiercingShot.asset");
            ArrowVolleySkillData volley = AssetDatabase.LoadAssetAtPath<ArrowVolleySkillData>(SkillDir + "HeroSkill_ArrowVolley.asset");
            EvadeSkillData evade = AssetDatabase.LoadAssetAtPath<EvadeSkillData>(SkillDir + "HeroSkill_Evade.asset");

            Assert.AreEqual(70, pierce.Damage);
            Assert.AreEqual(4f, pierce.Cooldown, 0.0001f);
            Assert.AreEqual(9f, pierce.LineLength, 0.0001f);
            Assert.AreEqual(0.6f, pierce.LineHalfWidth, 0.0001f);
            Assert.AreEqual(40, volley.DamagePerTick);
            Assert.AreEqual(3, volley.TickCount);
            Assert.AreEqual(0.5f, volley.TickIntervalSec, 0.0001f);
            Assert.AreEqual(2.8f, volley.Radius, 0.0001f);
            Assert.AreEqual(8f, volley.Cooldown, 0.0001f);
            Assert.AreEqual(30, evade.Damage);
            Assert.AreEqual(10f, evade.Cooldown, 0.0001f);
            Assert.AreEqual(4.5f, evade.LeapDistance, 0.0001f);
            Assert.AreEqual(0.25f, evade.LeapDurationSec, 0.0001f);
            Assert.AreEqual(2f, evade.BlastRadius, 0.0001f);
            Assert.AreEqual(3, evade.TriggerMonsterCount);
            Assert.AreEqual(2f, evade.TriggerRadius, 0.0001f);
        }

        [Test]
        public void asset_이름은_JSON_fileName과_같다()
        {
            foreach (string fileName in new[] { "HeroSkill_PiercingShot", "HeroSkill_ArrowVolley", "HeroSkill_Evade" })
            {
                HeroSkillData asset = AssetDatabase.LoadAssetAtPath<HeroSkillData>(SkillDir + fileName + ".asset");

                Assert.IsNotNull(asset, fileName);
                Assert.AreEqual(fileName, asset.name);
                Assert.IsNotNull(SkillJson(fileName));
            }
        }

        //# --- 로드아웃 ---

        private static List<JObject> LoadoutOf(string heroId)
        {
            List<JObject> list = new List<JObject>();
            foreach (JToken token in (JArray)Root()["loadout"])
            {
                if ((string)token["heroId"] == heroId)
                {
                    list.Add((JObject)token);
                }
            }
            return list;
        }

        private static void AssertLoadoutMatches(string heroId, string assetName)
        {
            HeroSkillLoadout asset = AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>(SkillDir + assetName);
            List<JObject> json = LoadoutOf(heroId);

            Assert.IsNotNull(asset, assetName);
            Assert.AreEqual(json.Count, asset.Phases.Count, $"{heroId} 페이즈 수");
            for (int i = 0; i < json.Count; ++i)
            {
                Assert.AreEqual((float)json[i]["hpFraction"], asset.Phases[i].HpFraction, 0.0001f, $"{heroId}[{i}] hp");
                Assert.AreEqual((string)json[i]["skill"], asset.Phases[i].Skill.name, $"{heroId}[{i}] skill");
            }
        }

        [Test]
        public void 궁수_로드아웃_asset은_JSON의_Archer_페이즈_3개와_일치한다()
        {
            AssertLoadoutMatches("Archer", "HeroSkillLoadout_Archer.asset");
        }

        [Test]
        public void 기사_로드아웃_asset은_JSON의_Knight_페이즈_3개와_일치한다()
        {
            AssertLoadoutMatches("Knight", "HeroSkillLoadout.asset");
        }

        [Test]
        public void 로드아웃의_heroId는_모두_EHero_값명이고_영웅별_페이즈는_3개다()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (JToken token in (JArray)Root()["loadout"])
            {
                string heroId = (string)token["heroId"];
                Assert.IsTrue(Enum.IsDefined(typeof(EHero), heroId), $"heroId {heroId}");
                counts[heroId] = counts.TryGetValue(heroId, out int c) ? c + 1 : 1;
            }

            Assert.AreEqual(3, counts["Knight"]);
            Assert.AreEqual(3, counts["Archer"]);
            Assert.AreEqual(2, counts.Count);
        }

        [Test]
        public void 로드아웃이_참조하는_스킬은_모두_skills에_정의돼_있다()
        {
            HashSet<string> defined = new HashSet<string>();
            foreach (JToken token in (JArray)Root()["skills"])
            {
                defined.Add((string)token["fileName"]);
            }

            foreach (JToken token in (JArray)Root()["loadout"])
            {
                Assert.IsTrue(defined.Contains((string)token["skill"]), (string)token["skill"]);
            }
        }

        [Test]
        public void 궁수_로드아웃은_HP_85_65_45_내림차순이고_스킬_게이트_값과_같다()
        {
            List<JObject> archer = LoadoutOf("Archer");
            float[] expected = { 0.85f, 0.65f, 0.45f };
            string[] skills = { "HeroSkill_PiercingShot", "HeroSkill_ArrowVolley", "HeroSkill_Evade" };

            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(expected[i], (float)archer[i]["hpFraction"], 0.0001f);
                Assert.AreEqual(skills[i], (string)archer[i]["skill"]);
            }
        }

        [Test]
        public void 기사와_궁수의_스킬_해금_HP는_같은_게이트_값을_쓴다()
        {
            List<JObject> knight = LoadoutOf("Knight");
            List<JObject> archer = LoadoutOf("Archer");

            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual((float)knight[i]["hpFraction"], (float)archer[i]["hpFraction"], 0.0001f);
            }
        }

        //# --- heroId 구형 JSON 호환 / DTO 왕복 ---

        [Test]
        public void heroId_키가_없는_페이즈_JSON은_기사로_간주된다()
        {
            HeroSkillPhaseDto phase = JsonConvert.DeserializeObject<HeroSkillPhaseDto>("{\"hpFraction\":0.85,\"skill\":\"HeroSkill_DashStrike\"}", JsonSyncSettings.Build());

            Assert.AreEqual("Knight", phase.HeroId);
            Assert.AreEqual(0.85f, phase.HpFraction, 0.0001f);
            Assert.AreEqual("HeroSkill_DashStrike", phase.Skill);
        }

        [Test]
        public void heroId_키가_있으면_그_값이_보존된다()
        {
            HeroSkillPhaseDto phase = JsonConvert.DeserializeObject<HeroSkillPhaseDto>("{\"heroId\":\"Archer\",\"hpFraction\":0.65,\"skill\":\"HeroSkill_ArrowVolley\"}", JsonSyncSettings.Build());

            Assert.AreEqual("Archer", phase.HeroId);
        }

        [Test]
        public void 구형_hero_skills_JSON_전체를_읽으면_로드아웃이_전부_기사로_해석된다()
        {
            JObject old = Root();
            JArray oldLoadout = new JArray();
            foreach (JToken token in (JArray)old["loadout"])
            {
                if ((string)token["heroId"] != "Knight")
                    continue;
                JObject copy = (JObject)token.DeepClone();
                copy.Remove("heroId");
                oldLoadout.Add(copy);
            }
            old["loadout"] = oldLoadout;

            HeroSkillsDto dto = JsonConvert.DeserializeObject<HeroSkillsDto>(old.ToString(), Settings());

            Assert.AreEqual(3, dto.Loadout.Count);
            foreach (HeroSkillPhaseDto phase in dto.Loadout)
            {
                Assert.AreEqual("Knight", phase.HeroId);
            }
            DestroySkills(dto);
        }

        [Test]
        public void DTO_왕복은_스킬_6종과_로드아웃_6페이즈를_보존한다()
        {
            JObject original = Root();
            HeroSkillsDto dto = JsonConvert.DeserializeObject<HeroSkillsDto>(original.ToString(), Settings());
            JObject back = JObject.Parse(JsonConvert.SerializeObject(dto, Settings()));

            JArray originalSkills = (JArray)original["skills"];
            JArray backSkills = (JArray)back["skills"];
            Assert.AreEqual(6, originalSkills.Count);
            Assert.AreEqual(originalSkills.Count, backSkills.Count);
            for (int i = 0; i < originalSkills.Count; ++i)
            {
                Assert.AreEqual((string)originalSkills[i]["$type"], (string)backSkills[i]["$type"], $"skills[{i}] 타입");
                Assert.AreEqual((string)originalSkills[i]["fileName"], (string)backSkills[i]["fileName"], $"skills[{i}] fileName");
                foreach (JProperty property in ((JObject)originalSkills[i]).Properties())
                {
                    JToken after = backSkills[i][property.Name];
                    Assert.IsNotNull(after, $"skills[{i}].{property.Name} 누락");
                    if (property.Value.Type == JTokenType.String)
                    {
                        Assert.AreEqual((string)property.Value, (string)after, $"skills[{i}].{property.Name}");
                    }
                    else
                    {
                        Assert.AreEqual((double)property.Value, (double)after, 0.0001, $"skills[{i}].{property.Name}");
                    }
                }
            }

            JArray originalLoadout = (JArray)original["loadout"];
            JArray backLoadout = (JArray)back["loadout"];
            Assert.AreEqual(6, backLoadout.Count);
            for (int i = 0; i < originalLoadout.Count; ++i)
            {
                Assert.AreEqual((string)originalLoadout[i]["heroId"], (string)backLoadout[i]["heroId"], $"loadout[{i}] heroId");
                Assert.AreEqual((double)originalLoadout[i]["hpFraction"], (double)backLoadout[i]["hpFraction"], 0.0001, $"loadout[{i}] hp");
                Assert.AreEqual((string)originalLoadout[i]["skill"], (string)backLoadout[i]["skill"], $"loadout[{i}] skill");
            }
            DestroySkills(dto);
        }

        [Test]
        public void 알_수_없는_스킬_타입이면_변환기가_JsonException을_던진다()
        {
            JObject broken = Root();
            ((JObject)broken["skills"][0])["$type"] = "NoSuchSkillData";

            Assert.Catch<JsonException>(() => JsonConvert.DeserializeObject<HeroSkillsDto>(broken.ToString(), Settings()));
        }

        //# --- 영웅 스탯 / 열거형 ---

        [Test]
        public void 궁수_스테이지_2_1_영웅_스탯은_HP_4992_공격력_44를_유지한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            HeroStats stats = HeroStatCalculator.Compute(4000, 50, db.GetHero("Archer"), db.GetStage(2, 1));

            Assert.AreEqual(4992, stats.Hp);
            Assert.AreEqual(44, stats.Power);
            Assert.AreEqual(6f, stats.Range, 0.0001f);
        }

        [Test]
        public void 궁수_FX_Enum_키가_정의돼_있다()
        {
            foreach (string name in new[] { "HeroPiercingShotFx", "HeroArrowVolleyFx", "HeroEvadeFx", "ArcherArrowFx" })
            {
                Assert.IsTrue(Enum.IsDefined(typeof(EVisual), name), name);
            }
            Assert.IsTrue(Enum.IsDefined(typeof(EHero), "Archer"));
        }
    }
}
