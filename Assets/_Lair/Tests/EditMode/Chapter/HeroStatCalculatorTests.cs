using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 영웅 스탯 도출(1회 반올림)·Knight Ch1 회귀·BalanceConfig 앵커 이관 (chapter-system §0.4·§1.2·§2.1).
    public class HeroStatCalculatorTests
    {
        private const string JsonDir = "Assets/_Lair/Data/Json/";
        private const int AnchorHp = 4000;
        private const int AnchorPower = 50;

        private static T[] Load<T>(string name)
        {
            return JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(JsonDir + name + ".json"));
        }

        private static HeroArchetypeRow ShippedHero(string heroId)
        {
            foreach (HeroArchetypeRow row in Load<HeroArchetypeRow>("HeroArchetype"))
            {
                if (row.heroId == heroId)
                    return row;
            }
            Assert.Fail($"HeroArchetype.json 에 {heroId} 없음");
            return null;
        }

        private static StageRow ShippedStage(int chapterId, int stageIndex)
        {
            foreach (StageRow row in Load<StageRow>("Stage"))
            {
                if (row.chapterId == chapterId && row.stageIndex == stageIndex)
                    return row;
            }
            Assert.Fail($"Stage.json 에 {chapterId}-{stageIndex} 없음");
            return null;
        }

        //# --- Knight Ch1 회귀 ---

        [Test]
        public void Knight_챕터1_HP는_앵커_곱_스테이지배수와_같다()
        {
            HeroArchetypeRow knight = ShippedHero("Knight");
            int[] expected = { 4000, 4400, 5440, 5840, 7680 };

            for (int index = 1; index <= 5; ++index)
            {
                HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, knight, ShippedStage(1, index));

                Assert.AreEqual(expected[index - 1], stats.Hp, $"1-{index} HP");
            }
        }

        [Test]
        public void Knight_챕터1_공격력은_기존_배수_그대로_유지된다()
        {
            HeroArchetypeRow knight = ShippedHero("Knight");
            int[] expected = { 50, 55, 60, 68, 75 };

            for (int index = 1; index <= 5; ++index)
            {
                HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, knight, ShippedStage(1, index));

                Assert.AreEqual(expected[index - 1], stats.Power, $"1-{index} Power");
            }
        }

        [Test]
        public void Knight_스테이지_계산은_직업배율_1이라_기존_ScaleStat_앵커x배수와_동일하다()
        {
            HeroArchetypeRow knight = ShippedHero("Knight");

            for (int index = 1; index <= 5; ++index)
            {
                StageRow stage = ShippedStage(1, index);
                HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, knight, stage);

                Assert.AreEqual(StageProgress.ScaleStat(AnchorHp, stage.hpMultiplier), stats.Hp, $"1-{index} HP");
                Assert.AreEqual(StageProgress.ScaleStat(AnchorPower, stage.powerMultiplier), stats.Power, $"1-{index} Power");
            }
        }

        [Test]
        public void Knight_사거리_쿨다운_이동속도는_영웅_직업_값_그대로다()
        {
            HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, ShippedHero("Knight"), ShippedStage(1, 1));

            Assert.AreEqual(1.5f, stats.Range, 0.0001f);
            Assert.AreEqual(1.0f, stats.Cooldown, 0.0001f);
            Assert.AreEqual(3.0f, stats.MoveSpeed, 0.0001f);
        }

        //# --- Archer ---

        [Test]
        public void Archer_스테이지_2_1은_HP_4992_공격력_44다()
        {
            HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, ShippedHero("Archer"), ShippedStage(2, 1));

            Assert.AreEqual(4992, stats.Hp);
            Assert.AreEqual(44, stats.Power);
        }

        [Test]
        public void Archer_스테이지_2_5는_HP_8160_공격력_64다()
        {
            HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, ShippedHero("Archer"), ShippedStage(2, 5));

            Assert.AreEqual(8160, stats.Hp);
            Assert.AreEqual(64, stats.Power);
        }

        [Test]
        public void Archer_사거리_쿨다운_이동속도는_직업_값_그대로다()
        {
            HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, ShippedHero("Archer"), ShippedStage(2, 3));

            Assert.AreEqual(6.0f, stats.Range, 0.0001f);
            Assert.AreEqual(1.0f, stats.Cooldown, 0.0001f);
            Assert.AreEqual(3.2f, stats.MoveSpeed, 0.0001f);
        }

        [Test]
        public void Archer_전_스테이지_HP와_공격력은_1_이상이다()
        {
            HeroArchetypeRow archer = ShippedHero("Archer");

            for (int index = 1; index <= 5; ++index)
            {
                HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, archer, ShippedStage(2, index));

                Assert.GreaterOrEqual(stats.Hp, 1);
                Assert.GreaterOrEqual(stats.Power, 1);
            }
        }

        //# --- 1회 반올림 / 경계 ---

        [Test]
        public void 중간_반올림을_하면_달라지는_조합에서_한번만_반올림한_값이_나온다()
        {
            HeroArchetypeRow archetype = new HeroArchetypeRow { heroId = "Knight", hpScale = 0.5f, powerScale = 0.5f, range = 1f, cooldown = 1f, moveSpeed = 1f };
            StageRow stage = new StageRow { hpMultiplier = 1.2f, powerMultiplier = 1.2f };

            HeroStats stats = HeroStatCalculator.Compute(5, 5, archetype, stage);

            Assert.AreEqual(3, stats.Hp, "5 x 0.6 = 3.0 (이중 반올림이면 4)");
            Assert.AreEqual(3, stats.Power);
        }

        [Test]
        public void HP와_공격력은_각각_자기_배수만_사용한다()
        {
            HeroArchetypeRow archetype = new HeroArchetypeRow { hpScale = 2f, powerScale = 0.5f, range = 1f, cooldown = 1f, moveSpeed = 1f };
            StageRow stage = new StageRow { hpMultiplier = 1f, powerMultiplier = 3f };

            HeroStats stats = HeroStatCalculator.Compute(100, 100, archetype, stage);

            Assert.AreEqual(200, stats.Hp);
            Assert.AreEqual(150, stats.Power);
        }

        [Test]
        public void 배수가_0이면_스탯은_최소_1로_보정된다()
        {
            HeroArchetypeRow archetype = new HeroArchetypeRow { hpScale = 0f, powerScale = 0f, range = 1f, cooldown = 1f, moveSpeed = 1f };
            StageRow stage = new StageRow { hpMultiplier = 1f, powerMultiplier = 1f };

            HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, archetype, stage);

            Assert.AreEqual(1, stats.Hp);
            Assert.AreEqual(1, stats.Power);
        }

        //# --- BalanceConfig 앵커 이관 ---

        [Test]
        public void HeroAnchor는_Hp와_Power_두_필드만_가진다()
        {
            FieldInfo[] fields = typeof(BalanceConfig.HeroAnchor).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            List<string> names = new List<string>();
            foreach (FieldInfo field in fields)
            {
                names.Add(field.Name);
            }
            names.Sort();
            CollectionAssert.AreEqual(new[] { "Hp", "Power" }, names);
        }

        [Test]
        public void BalanceConfig_에셋의_영웅_앵커는_4000_50이다()
        {
            BalanceConfig config = AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/_Lair/Data/BalanceConfig.asset");

            Assert.IsNotNull(config, "BalanceConfig.asset 로드 실패");
            Assert.AreEqual(AnchorHp, config.Hero.Hp);
            Assert.AreEqual(AnchorPower, config.Hero.Power);
        }

        [Test]
        public void balance_config_json의_hero는_hp_power만_가진다()
        {
            JObject root = JObject.Parse(File.ReadAllText(JsonDir + "balance_config.json"));
            JObject hero = root["hero"] as JObject;

            Assert.IsNotNull(hero);
            List<string> keys = new List<string>();
            foreach (JProperty property in hero.Properties())
            {
                keys.Add(property.Name);
            }
            keys.Sort();
            CollectionAssert.AreEqual(new[] { "hp", "power" }, keys);
            Assert.AreEqual(AnchorHp, hero["hp"].Value<int>());
            Assert.AreEqual(AnchorPower, hero["power"].Value<int>());
        }

        [Test]
        public void balance_config_json_앵커와_BalanceConfig_에셋_앵커가_일치한다()
        {
            JObject hero = JObject.Parse(File.ReadAllText(JsonDir + "balance_config.json"))["hero"] as JObject;
            BalanceConfig config = AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/_Lair/Data/BalanceConfig.asset");

            Assert.AreEqual(hero["hp"].Value<int>(), config.Hero.Hp);
            Assert.AreEqual(hero["power"].Value<int>(), config.Hero.Power);
        }

        [Test]
        public void HeroArchetype_json의_Knight는_이관된_기존_영웅_스탯과_같다()
        {
            HeroArchetypeRow knight = ShippedHero("Knight");

            Assert.AreEqual(1.0f, knight.hpScale, 0.0001f);
            Assert.AreEqual(1.0f, knight.powerScale, 0.0001f);
            Assert.AreEqual("Melee", knight.aiType);
        }
    }
}
