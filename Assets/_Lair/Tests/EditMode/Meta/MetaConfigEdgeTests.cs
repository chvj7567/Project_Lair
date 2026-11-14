using System;
using System.Collections.Generic;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# MetaConfig.Build 검증 경계·관계 행 규칙·조립 기본값 (so-to-json-migration §3.4~§3.8·§7.2·§9 E3·§10 SJ3~SJ4). 합성 row 만 쓴다.
    public class MetaConfigEdgeTests
    {
        //# Strings_Ko 모사 — id 99 는 "없는 id", 그 외 양수는 존재. 표시 문구는 "T" + id.
        private static bool Exists(int id) => id > 0 && id != 99;

        private static string TextOf(int id) => "T" + id;

        private static MetaConfigRow Constants()
        {
            return new MetaConfigRow { winBaseSouls = 100, winTimeBonusPerSec = 0.5f, loseMaxSouls = 60, winXp = 100, loseXp = 40, xpPerLevelBase = 100, xpGrowth = 1.25f };
        }

        private static ShopItemRow Item(string id, string kind, string currency = "Soul")
        {
            return new ShopItemRow { itemId = id, nameKey = 1, effectKind = kind, maxLevel = 3, basePrice = 10, priceGrowth = 1.5f, currency = currency };
        }

        //# 상점 1종(MonsterStat + Stat·Mul 행) · 영주 칸 3(보상은 2 만) · 도전과제 1 — 위반 0 인 최소 묶음.
        private static MetaDataSet Minimal()
        {
            return new MetaDataSet
            {
                Config = Constants(),
                ShopItems = new[] { Item("Hp", "MonsterStat") },
                Stats = new[] { new ShopItem_StatRow { itemId = "Hp", statKind = "Power" } },
                Species = new ShopItem_SpeciesRow[0],
                Muls = new[] { new ShopItem_MulRow { itemId = "Hp", perLevelMul = 1.02f } },
                Descs = new ShopItem_DescRow[0],
                LordLevels = new[] { new LordLevelRow { level = 1 }, new LordLevelRow { level = 2 }, new LordLevelRow { level = 3 } },
                LordRewards = new[] { new LordLevel_RewardRow { level = 2, nameKey = 5, rewardSouls = 50 } },
                Achievements = new[] { Achievement("A1") },
            };
        }

        private static AchievementRow Achievement(string id)
        {
            return new AchievementRow { achievementId = id, nameKey = 2, descKey = 3, condition = "TotalWins", threshold = 5f, rewardSouls = 40 };
        }

        private static MetaConfig Build(MetaDataSet data, Func<int, bool> exists = null, Func<ECurrency, bool> currencyExists = null)
        {
            Func<int, bool> existsCheck = exists != null ? exists : new Func<int, bool>(Exists);
            Func<ECurrency, bool> currencyCheck = currencyExists != null ? currencyExists : new Func<ECurrency, bool>(currency => true);
            return MetaConfig.Build(data, existsCheck, TextOf, currencyCheck);
        }

        private static void AssertBuilds(MetaDataSet data)
        {
            Assert.DoesNotThrow(() => Build(data));
        }

        private static void AssertFails(MetaDataSet data, params string[] fragments)
        {
            BuildEdgeTestKit.AssertFails(() => Build(data), fragments);
        }

        //# --- 정상 조립 · 기본값 ---

        [Test]
        public void 최소_묶음은_통과하고_문구와_값이_row에서_조립된다()
        {
            MetaConfig meta = Build(Minimal());

            Assert.AreEqual(100, meta.WinBaseSouls);
            Assert.AreEqual(0.5f, meta.WinTimeBonusPerSec, 0f);
            Assert.AreEqual(60, meta.LoseMaxSouls);
            Assert.AreEqual(100, meta.WinXp);
            Assert.AreEqual(40, meta.LoseXp);
            Assert.AreEqual(100, meta.XpPerLevelBase);
            Assert.AreEqual(1.25f, meta.XpGrowth, 0f);
            ShopItemDef item = meta.ShopItems[0];
            Assert.AreEqual("Hp", item.Id);
            Assert.AreEqual("T1", item.DisplayName);
            Assert.AreEqual(EShopEffectKind.MonsterStat, item.EffectKind);
            Assert.AreEqual(EMonsterStatKind.Power, item.StatKind);
            Assert.AreEqual(1.02f, item.PerLevelMul, 0f);
            Assert.AreEqual(3, item.MaxLevel);
            Assert.AreEqual(10, item.BasePrice);
            Assert.AreEqual(1.5f, item.PriceGrowth, 0f);
            Assert.AreEqual(ECurrency.Soul, item.Currency);
            AchievementDef achievement = meta.Achievements[0];
            Assert.AreEqual("T2", achievement.DisplayName);
            Assert.AreEqual("T3", achievement.Description);
            Assert.AreEqual(EAchievementCondition.TotalWins, achievement.Condition);
            Assert.AreEqual(5f, achievement.Threshold, 0f);
            Assert.AreEqual(40, achievement.RewardSouls);
        }

        //# 관계 행이 없는 항목은 SO 의 직렬화 기본값(Hp / Wisp / 1.0 / 빈 설명)과 같아야 한다 (§7.2).
        [Test]
        public void 관계_행이_없으면_StatKind_Hp_Species_Wisp_PerLevelMul_1_설명_빈값이다()
        {
            MetaDataSet data = Minimal();
            data.ShopItems = new[] { Item("Haste", "SpawnerPeriod"), Item("Awaken_Hex", "MonsterAwaken") };
            data.Stats = new ShopItem_StatRow[0];
            data.Species = new[] { new ShopItem_SpeciesRow { itemId = "Awaken_Hex", monster = "Hex" } };
            data.Muls = new[] { new ShopItem_MulRow { itemId = "Haste", perLevelMul = 0.985f } };

            MetaConfig meta = Build(data);

            ShopItemDef haste = meta.FindShopItem("Haste");
            Assert.AreEqual(EMonsterStatKind.Hp, haste.StatKind);
            Assert.AreEqual(EMonster.Wisp, haste.Species);
            Assert.AreEqual(0.985f, haste.PerLevelMul, 0f);
            Assert.AreEqual(string.Empty, haste.Description);
            ShopItemDef awaken = meta.FindShopItem("Awaken_Hex");
            Assert.AreEqual(EMonster.Hex, awaken.Species);
            Assert.AreEqual(1f, awaken.PerLevelMul, 0f);
            Assert.AreEqual(EMonsterStatKind.Hp, awaken.StatKind);
            Assert.AreEqual(string.Empty, awaken.Description);
        }

        [Test]
        public void 설명_행이_있으면_Strings_문구가_Description이_된다()
        {
            MetaDataSet data = Minimal();
            data.Descs = new[] { new ShopItem_DescRow { itemId = "Hp", descKey = 7 } };

            Assert.AreEqual("T7", Build(data).ShopItems[0].Description);
        }

        [Test]
        public void 상점_항목_표시_순서는_입력_행_순서와_같다()
        {
            MetaDataSet data = Minimal();
            data.ShopItems = new[] { Item("Zeta", "MonsterAwaken"), Item("Alpha", "MonsterAwaken"), Item("Mid", "MonsterAwaken") };
            data.Stats = new ShopItem_StatRow[0];
            data.Muls = new ShopItem_MulRow[0];
            data.Species = new[]
            {
                new ShopItem_SpeciesRow { itemId = "Mid", monster = "Wisp" },
                new ShopItem_SpeciesRow { itemId = "Zeta", monster = "Wisp" },
                new ShopItem_SpeciesRow { itemId = "Alpha", monster = "Wisp" },
            };

            MetaConfig meta = Build(data);

            Assert.AreEqual("Zeta", meta.ShopItems[0].Id);
            Assert.AreEqual("Alpha", meta.ShopItems[1].Id);
            Assert.AreEqual("Mid", meta.ShopItems[2].Id);
        }

        [Test]
        public void 표시_문구가_null로_조회되면_빈_문자열이다()
        {
            MetaConfig meta = MetaConfig.Build(Minimal(), Exists, id => null, currency => true);

            Assert.AreEqual(string.Empty, meta.ShopItems[0].DisplayName);
            Assert.AreEqual(string.Empty, meta.LordRewards[1].DisplayName);
            Assert.AreEqual(string.Empty, meta.Achievements[0].Description);
        }

        //# Strings_Ko 미등록(E3) — 존재 검사는 통과시키고 표시 문자열은 빈 값.
        [Test]
        public void 문구_테이블이_비어도_존재_검사가_통과면_조립되고_문구는_빈값이다()
        {
            MetaConfig meta = MetaConfig.Build(Minimal(), id => true, id => string.Empty, currency => true);

            Assert.AreEqual(string.Empty, meta.ShopItems[0].DisplayName);
        }

        //# --- 필수 입력 ---

        [Test]
        public void 묶음_또는_상수_객체가_null이면_비어_있음_오류다()
        {
            BuildEdgeTestKit.AssertFails(() => MetaConfig.Build(null, Exists, TextOf, currency => true), "MetaConfig.json 이 비어 있음");
            BuildEdgeTestKit.AssertFails(() => Build(new MetaDataSet()), "MetaConfig.json 이 비어 있음");
        }

        [Test]
        public void 표_배열이_전부_null이면_세_엔티티_비어_있음을_함께_보고한다()
        {
            MetaDataSet data = new MetaDataSet { Config = Constants() };

            AssertFails(data, "ShopItem.json 이 비어 있음", "LordLevel.json 이 비어 있음", "Achievement.json 이 비어 있음");
        }

        [Test]
        public void 표_배열이_비어_있어도_세_엔티티_비어_있음을_보고한다()
        {
            MetaDataSet data = Minimal();
            data.ShopItems = new ShopItemRow[0];
            data.Stats = new ShopItem_StatRow[0];
            data.Muls = new ShopItem_MulRow[0];
            data.LordLevels = new LordLevelRow[0];
            data.LordRewards = new LordLevel_RewardRow[0];
            data.Achievements = new AchievementRow[0];

            AssertFails(data, "ShopItem.json 이 비어 있음", "LordLevel.json 이 비어 있음", "Achievement.json 이 비어 있음");
        }

        //# --- 상수 7개 경계 ---

        [TestCase("winBaseSouls", 1.0, true)]
        [TestCase("winBaseSouls", 0.0, false)]
        [TestCase("winBaseSouls", -1.0, false)]
        [TestCase("loseMaxSouls", 1.0, true)]
        [TestCase("loseMaxSouls", 0.0, false)]
        [TestCase("winXp", 1.0, true)]
        [TestCase("winXp", 0.0, false)]
        [TestCase("loseXp", 1.0, true)]
        [TestCase("loseXp", 0.0, false)]
        [TestCase("xpPerLevelBase", 1.0, true)]
        [TestCase("xpPerLevelBase", 0.0, false)]
        [TestCase("xpPerLevelBase", -100.0, false)]
        public void 정수_상수는_1_이상이어야_한다(string field, double value, bool ok)
        {
            MetaDataSet data = Minimal();
            typeof(MetaConfigRow).GetField(field).SetValue(data.Config, Convert.ChangeType(value, typeof(int)));

            if (ok)
            {
                AssertBuilds(data);
                return;
            }
            AssertFails(data, "winBaseSouls·loseMaxSouls·winXp·loseXp·xpPerLevelBase 는 1 이상이어야 함");
        }

        [TestCase(0.0001, true)]
        [TestCase(0.5, true)]
        [TestCase(0.0, false)]
        [TestCase(-0.5, false)]
        public void 승리_시간_보너스는_0보다_커야_한다(double value, bool ok)
        {
            MetaDataSet data = Minimal();
            data.Config.winTimeBonusPerSec = (float)value;

            if (ok)
            {
                AssertBuilds(data);
                return;
            }
            AssertFails(data, "winTimeBonusPerSec 는 0 보다 커야 함");
        }

        [TestCase(1.0, true)]
        [TestCase(1.25, true)]
        [TestCase(3.0, true)]
        [TestCase(0.999, false)]
        [TestCase(0.0, false)]
        [TestCase(-1.0, false)]
        public void 영주_XP_성장률은_1_이상이어야_한다(double value, bool ok)
        {
            MetaDataSet data = Minimal();
            data.Config.xpGrowth = (float)value;

            if (ok)
            {
                AssertBuilds(data);
                return;
            }
            AssertFails(data, "xpGrowth 는 1 이상이어야 함");
        }

        //# --- ShopItem 필드 경계 ---

        [TestCase(3, 10, 1.5, true)]
        [TestCase(1, 1, 1.0, true)]
        [TestCase(int.MaxValue, int.MaxValue, 100.0, true)]
        [TestCase(-1, 10, 1.5, false)]
        [TestCase(3, 0, 1.5, false)]
        [TestCase(3, -1, 1.5, false)]
        [TestCase(3, 10, 0.99, false)]
        [TestCase(3, 10, 0.0, false)]
        public void 상점_항목의_최대_레벨_가격_성장률_경계(int maxLevel, int basePrice, double growth, bool ok)
        {
            MetaDataSet data = Minimal();
            data.ShopItems[0].maxLevel = maxLevel;
            data.ShopItems[0].basePrice = basePrice;
            data.ShopItems[0].priceGrowth = (float)growth;

            if (ok)
            {
                AssertBuilds(data);
                return;
            }
            AssertFails(data, "ShopItem[Hp] maxLevel·basePrice 는 1 이상, priceGrowth 는 1 이상이어야 함");
        }

        [TestCase(0.0001, true)]
        [TestCase(1.0, true)]
        [TestCase(50.0, true)]
        [TestCase(0.0, false)]
        [TestCase(-1.0, false)]
        public void 레벨당_배율은_0보다_커야_한다(double mul, bool ok)
        {
            MetaDataSet data = Minimal();
            data.Muls[0].perLevelMul = (float)mul;

            if (ok)
            {
                AssertBuilds(data);
                return;
            }
            AssertFails(data, "ShopItem_Mul[Hp].perLevelMul 은 0 보다 커야 함");
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(99)]
        public void 상점_항목_nameKey가_0_음수_또는_없는_id면_위반이다(int nameKey)
        {
            MetaDataSet data = Minimal();
            data.ShopItems[0].nameKey = nameKey;

            AssertFails(data, "ShopItem[Hp].nameKey " + nameKey + " 가 Strings_Ko 에 없음");
        }

        [TestCase(0)]
        [TestCase(-3)]
        [TestCase(99)]
        public void 설명_행의_descKey가_0_음수_또는_없는_id면_위반이다(int descKey)
        {
            MetaDataSet data = Minimal();
            data.Descs = new[] { new ShopItem_DescRow { itemId = "Hp", descKey = descKey } };

            AssertFails(data, "ShopItem_Desc[Hp].descKey " + descKey + " 가 Strings_Ko 에 없음");
        }

        [TestCase("Unknown")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("0")]
        [TestCase("monsterstat")]
        public void effectKind가_값명이_아니면_위반이다(string kind)
        {
            MetaDataSet data = Minimal();
            data.ShopItems[0].effectKind = kind;

            AssertFails(data, "ShopItem[Hp].effectKind '" + kind + "' 가 EShopEffectKind 에 없음");
        }

        [TestCase("Gold")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("0")]
        [TestCase("soul")]
        public void 결제_재화가_값명이_아니면_위반이다(string currency)
        {
            MetaDataSet data = Minimal();
            data.ShopItems[0].currency = currency;

            AssertFails(data, "ShopItem[Hp].currency '" + currency + "' 가 ECurrency·Currency.json 에 없음");
        }

        [Test]
        public void 재화_값명이_맞아도_재화_표에_없으면_위반이다()
        {
            MetaDataSet data = Minimal();
            data.ShopItems[0].currency = "MoonSeal";

            BuildEdgeTestKit.AssertFails(() => Build(data, null, currency => currency != ECurrency.MoonSeal),
                "ShopItem[Hp].currency 'MoonSeal'");
        }

        [Test]
        public void ECurrency_5종은_재화_표에_있으면_모두_통과한다()
        {
            foreach (string name in Enum.GetNames(typeof(ECurrency)))
            {
                MetaDataSet data = Minimal();
                data.ShopItems[0].currency = name;

                Assert.AreEqual((ECurrency)Enum.Parse(typeof(ECurrency), name), Build(data).ShopItems[0].Currency, name);
            }
        }

        //# --- 관계 행 ---

        [TestCase("Bogus")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("5")]
        [TestCase("hp")]
        public void statKind가_값명이_아니면_위반이다(string statKind)
        {
            MetaDataSet data = Minimal();
            data.Stats[0].statKind = statKind;

            AssertFails(data, "ShopItem_Stat[Hp].statKind '" + statKind + "' 가 EMonsterStatKind 에 없음");
        }

        [Test]
        public void statKind_9종은_모두_그대로_조립된다()
        {
            foreach (string name in Enum.GetNames(typeof(EMonsterStatKind)))
            {
                MetaDataSet data = Minimal();
                data.Stats[0].statKind = name;

                Assert.AreEqual((EMonsterStatKind)Enum.Parse(typeof(EMonsterStatKind), name), Build(data).ShopItems[0].StatKind, name);
            }
        }

        [TestCase("Ghost")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("WispG2")]
        [TestCase("PhantomG5")]
        [TestCase("1")]
        [TestCase("wisp")]
        public void 종족_행의_monster가_기준_6종_밖이면_위반이다(string monster)
        {
            MetaDataSet data = Minimal();
            data.ShopItems = new[] { Item("Enhance_X", "MonsterAwaken") };
            data.Stats = new ShopItem_StatRow[0];
            data.Muls = new ShopItem_MulRow[0];
            data.Species = new[] { new ShopItem_SpeciesRow { itemId = "Enhance_X", monster = monster } };

            AssertFails(data, "ShopItem_Species[Enhance_X].monster '" + monster + "' 는 EMonster 기준 6종 값명이어야 함");
        }

        [TestCase("Wisp")]
        [TestCase("Wraith")]
        [TestCase("Reaper")]
        [TestCase("Hex")]
        [TestCase("Plague")]
        [TestCase("Phantom")]
        public void 종족_행의_monster_기준_6종은_통과한다(string monster)
        {
            MetaDataSet data = Minimal();
            data.ShopItems = new[] { Item("Trait_X", "MonsterTrait", "AbyssCrystal") };
            data.Stats = new ShopItem_StatRow[0];
            data.Muls = new ShopItem_MulRow[0];
            data.Species = new[] { new ShopItem_SpeciesRow { itemId = "Trait_X", monster = monster } };

            Assert.AreEqual((EMonster)Enum.Parse(typeof(EMonster), monster), Build(data).ShopItems[0].Species);
        }

        [Test]
        public void 관계_행의_itemId가_ShopItem에_없으면_FK_오류다()
        {
            MetaDataSet data = Minimal();
            data.Stats = new[] { new ShopItem_StatRow { itemId = "Hp", statKind = "Hp" }, new ShopItem_StatRow { itemId = "Ghost", statKind = "Hp" } };
            data.Descs = new[] { new ShopItem_DescRow { itemId = "Phantom", descKey = 7 } };

            AssertFails(data, "ShopItem_Stat.json itemId Ghost 가 ShopItem.json 에 없음", "ShopItem_Desc.json itemId Phantom 가 ShopItem.json 에 없음");
        }

        [Test]
        public void 관계_행의_PK가_중복이면_오류다()
        {
            MetaDataSet data = Minimal();
            data.Muls = new[] { new ShopItem_MulRow { itemId = "Hp", perLevelMul = 1.1f }, new ShopItem_MulRow { itemId = "Hp", perLevelMul = 1.2f } };
            data.Descs = new[] { new ShopItem_DescRow { itemId = "Hp", descKey = 7 }, new ShopItem_DescRow { itemId = "Hp", descKey = 8 } };

            AssertFails(data, "ShopItem_Mul.json itemId Hp 중복", "ShopItem_Desc.json itemId Hp 중복");
        }

        [Test]
        public void 관계_행이_null이거나_itemId가_비어_있으면_오류로_모은다()
        {
            MetaDataSet data = Minimal();
            data.Stats = new[] { new ShopItem_StatRow { itemId = "Hp", statKind = "Hp" }, null, new ShopItem_StatRow { itemId = "", statKind = "Hp" } };

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            StringAssert.Contains("ShopItem_Stat.json itemId 가 비어 있음", e.Message);
            Assert.AreEqual(2, BuildEdgeTestKit.Violations(e).Length);
        }

        //# effectKind x (Stat·Species·Mul) 8가지 조합 — §3.6 표(필수/금지)와 독립으로 다시 적은 기대값으로 누락·초과를 정확히 센다.
        private static readonly Dictionary<string, bool[]> RelationNeeds = new Dictionary<string, bool[]>
        {
            { "MonsterStat", new[] { true, false, true } },
            { "SpawnerPeriod", new[] { false, false, true } },
            { "MonsterSpecies", new[] { false, true, true } },
            { "MonsterAwaken", new[] { false, true, false } },
            { "NestMastery", new[] { false, true, false } },
            { "MonsterTrait", new[] { false, true, false } },
        };

        private static IEnumerable<TestCaseData> RelationMatrix()
        {
            foreach (string kind in Enum.GetNames(typeof(EShopEffectKind)))
            {
                for (int mask = 0; mask < 8; mask++)
                {
                    bool stat = (mask & 1) != 0;
                    bool species = (mask & 2) != 0;
                    bool mul = (mask & 4) != 0;
                    yield return new TestCaseData(kind, stat, species, mul).SetName($"관계_행_규칙_{kind}_Stat{stat}_Species{species}_Mul{mul}");
                }
            }
        }

        [TestCaseSource(nameof(RelationMatrix))]
        public void effectKind_별_관계_행_필수_금지_규칙이_정확히_적용된다(string kind, bool hasStat, bool hasSpecies, bool hasMul)
        {
            MetaDataSet data = Minimal();
            data.ShopItems = new[] { Item("X", kind) };
            data.Stats = hasStat ? new[] { new ShopItem_StatRow { itemId = "X", statKind = "Hp" } } : new ShopItem_StatRow[0];
            data.Species = hasSpecies ? new[] { new ShopItem_SpeciesRow { itemId = "X", monster = "Wisp" } } : new ShopItem_SpeciesRow[0];
            data.Muls = hasMul ? new[] { new ShopItem_MulRow { itemId = "X", perLevelMul = 1.1f } } : new ShopItem_MulRow[0];
            bool[] needs = RelationNeeds[kind];
            bool[] has = { hasStat, hasSpecies, hasMul };
            string[] labels = { "ShopItem_Stat", "ShopItem_Species", "ShopItem_Mul" };
            List<string> expected = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                if (needs[i] && has[i] == false)
                {
                    expected.Add($"{labels[i]}.json 에 X({kind}) 행이 없음");
                }
                else if (needs[i] == false && has[i])
                {
                    expected.Add($"{labels[i]}.json 에 X({kind}) 행이 있으면 안 됨");
                }
            }

            if (expected.Count == 0)
            {
                AssertBuilds(data);
                return;
            }
            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));
            foreach (string fragment in expected)
            {
                StringAssert.Contains(fragment, e.Message);
            }
            Assert.AreEqual(expected.Count, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }

        //# --- ShopItem PK ---

        [Test]
        public void 상점_itemId가_중복이면_오류이고_첫_행만_남는다()
        {
            MetaDataSet data = Minimal();
            data.ShopItems = new[] { Item("Hp", "MonsterStat"), Item("Hp", "MonsterStat") };

            AssertFails(data, "ShopItem.json itemId Hp 중복");
        }

        [Test]
        public void 상점_행이_null이거나_itemId가_비어_있으면_오류다()
        {
            MetaDataSet data = Minimal();
            data.ShopItems = new[] { Item("Hp", "MonsterStat"), null, Item("", "MonsterStat"), Item(null, "MonsterStat") };

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            Assert.AreEqual(3, BuildEdgeTestKit.Violations(e).Length, e.Message);
            StringAssert.Contains("ShopItem.json itemId 가 비어 있음", e.Message);
        }

        [Test]
        public void effectKind가_틀린_항목은_관계_행_규칙_검사를_건너뛴다()
        {
            MetaDataSet data = Minimal();
            data.ShopItems[0].effectKind = "Nope";
            data.Stats = new ShopItem_StatRow[0];
            data.Muls = new ShopItem_MulRow[0];

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            Assert.AreEqual(1, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }

        //# --- LordLevel ---

        [TestCase(0)]
        [TestCase(-1)]
        public void 영주_칸_level이_1_미만이면_위반이다(int level)
        {
            MetaDataSet data = Minimal();
            data.LordLevels = new[] { new LordLevelRow { level = level }, new LordLevelRow { level = 2 } };

            AssertFails(data, "LordLevel.json level 은 1 이상이어야 함");
        }

        [Test]
        public void 영주_칸_행이_null이면_위반이다()
        {
            MetaDataSet data = Minimal();
            data.LordLevels = new[] { new LordLevelRow { level = 2 }, null };

            AssertFails(data, "LordLevel.json level 은 1 이상이어야 함");
        }

        [Test]
        public void 영주_칸_level이_중복이거나_내림차순이면_위반이다()
        {
            MetaDataSet duplicate = Minimal();
            duplicate.LordLevels = new[] { new LordLevelRow { level = 2 }, new LordLevelRow { level = 2 } };
            MetaDataSet descending = Minimal();
            descending.LordLevels = new[] { new LordLevelRow { level = 3 }, new LordLevelRow { level = 2 } };

            AssertFails(duplicate, "LordLevel.json level 2 는 중복 없이 오름차순이어야 함");
            AssertFails(descending, "LordLevel.json level 2 는 중복 없이 오름차순이어야 함");
        }

        [Test]
        public void 영주_칸이_1부터가_아니거나_건너뛰어도_오름차순이면_통과하고_행_순서를_지킨다()
        {
            MetaDataSet data = Minimal();
            data.LordLevels = new[] { new LordLevelRow { level = 2 }, new LordLevelRow { level = 5 }, new LordLevelRow { level = 9 } };
            data.LordRewards = new[] { new LordLevel_RewardRow { level = 5, nameKey = 5, rewardSouls = 10 } };

            MetaConfig meta = Build(data);

            Assert.AreEqual(3, meta.LordRewards.Count);
            Assert.AreEqual(2, meta.LordRewards[0].Level);
            Assert.AreEqual(5, meta.LordRewards[1].Level);
            Assert.AreEqual(9, meta.LordRewards[2].Level);
        }

        //# 보상 행이 없는 칸 = 잠금 칸 — IsLockedDummy true · 소울 0 · 이름 빈 값 (§3.7).
        [Test]
        public void 보상_행이_없는_칸은_잠금_칸이고_있는_칸은_보상_값을_가진다()
        {
            MetaConfig meta = Build(Minimal());

            LordRewardDef locked1 = meta.LordRewards[0];
            LordRewardDef real = meta.LordRewards[1];
            LordRewardDef locked3 = meta.LordRewards[2];
            Assert.IsTrue(locked1.IsLockedDummy);
            Assert.AreEqual(0, locked1.RewardSouls);
            Assert.AreEqual(string.Empty, locked1.DisplayName);
            Assert.AreEqual(1, locked1.Level);
            Assert.IsFalse(real.IsLockedDummy);
            Assert.AreEqual(50, real.RewardSouls);
            Assert.AreEqual("T5", real.DisplayName);
            Assert.AreEqual(2, real.Level);
            Assert.IsTrue(locked3.IsLockedDummy);
            Assert.AreEqual(3, locked3.Level);
        }

        [Test]
        public void 보상_행이_null이거나_비어_있으면_모든_칸이_잠금이다()
        {
            foreach (LordLevel_RewardRow[] rewards in new LordLevel_RewardRow[][] { null, new LordLevel_RewardRow[0] })
            {
                MetaDataSet data = Minimal();
                data.LordRewards = rewards;

                MetaConfig meta = Build(data);

                Assert.AreEqual(3, meta.LordRewards.Count);
                foreach (LordRewardDef def in meta.LordRewards)
                {
                    Assert.IsTrue(def.IsLockedDummy, "level " + def.Level);
                }
            }
        }

        [Test]
        public void 보상_행의_level이_영주_칸에_없으면_FK_오류다()
        {
            MetaDataSet data = Minimal();
            data.LordRewards = new[] { new LordLevel_RewardRow { level = 2, nameKey = 5, rewardSouls = 50 }, new LordLevel_RewardRow { level = 9, nameKey = 5, rewardSouls = 50 } };

            AssertFails(data, "LordLevel_Reward.json level 9 가 LordLevel.json 에 없음");
        }

        [Test]
        public void 보상_행이_null이거나_level이_중복이면_오류다()
        {
            MetaDataSet nullRow = Minimal();
            nullRow.LordRewards = new LordLevel_RewardRow[] { null };
            MetaDataSet duplicate = Minimal();
            duplicate.LordRewards = new[] { new LordLevel_RewardRow { level = 2, nameKey = 5, rewardSouls = 50 }, new LordLevel_RewardRow { level = 2, nameKey = 6, rewardSouls = 60 } };

            AssertFails(nullRow, "LordLevel_Reward.json level");
            AssertFails(duplicate, "LordLevel_Reward.json level 2 중복");
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(99)]
        public void 보상_이름_키가_0_음수_없는_id면_위반이다(int nameKey)
        {
            MetaDataSet data = Minimal();
            data.LordRewards[0].nameKey = nameKey;

            AssertFails(data, "LordLevel_Reward[2].nameKey " + nameKey + " 가 Strings_Ko 에 없음");
        }

        [Test]
        public void 보상_소울은_음수면_위반이고_1이면_통과한다()
        {
            MetaDataSet negative = Minimal();
            negative.LordRewards[0].rewardSouls = -1;
            MetaDataSet one = Minimal();
            one.LordRewards[0].rewardSouls = 1;

            AssertFails(negative, "LordLevel_Reward[2].rewardSouls");
            Assert.AreEqual(1, Build(one).LordRewards[1].RewardSouls);
        }

        //# --- Achievement ---

        [Test]
        public void 도전과제_id가_중복이면_오류다()
        {
            MetaDataSet data = Minimal();
            data.Achievements = new[] { Achievement("A1"), Achievement("A1") };

            AssertFails(data, "Achievement.json achievementId A1 중복");
        }

        [Test]
        public void 도전과제_행이_null이거나_id가_비어_있으면_오류다()
        {
            MetaDataSet data = Minimal();
            data.Achievements = new[] { Achievement("A1"), null, Achievement(""), Achievement(null) };

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            Assert.AreEqual(3, BuildEdgeTestKit.Violations(e).Length, e.Message);
            StringAssert.Contains("Achievement.json achievementId 가 비어 있음", e.Message);
        }

        [TestCase("Unknown")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("0")]
        [TestCase("totalwins")]
        public void 도전과제_condition이_값명이_아니면_위반이다(string condition)
        {
            MetaDataSet data = Minimal();
            data.Achievements[0].condition = condition;

            AssertFails(data, "Achievement[A1].condition '" + condition + "' 가 EAchievementCondition 에 없음");
        }

        [Test]
        public void 도전과제_condition_5종은_모두_통과한다()
        {
            foreach (string name in Enum.GetNames(typeof(EAchievementCondition)))
            {
                MetaDataSet data = Minimal();
                data.Achievements[0].condition = name;

                Assert.AreEqual((EAchievementCondition)Enum.Parse(typeof(EAchievementCondition), name), Build(data).Achievements[0].Condition, name);
            }
        }

        [TestCase(0.0001, true)]
        [TestCase(1.0, true)]
        [TestCase(0.0, false)]
        [TestCase(-1.0, false)]
        public void 도전과제_임계는_0보다_커야_한다(double threshold, bool ok)
        {
            MetaDataSet data = Minimal();
            data.Achievements[0].threshold = (float)threshold;

            if (ok)
            {
                AssertBuilds(data);
                return;
            }
            AssertFails(data, "Achievement[A1] threshold 는 0 보다 크고 rewardSouls 는 1 이상이어야 함");
        }

        [Test]
        public void 도전과제_보상은_음수면_위반이고_1이면_통과한다()
        {
            MetaDataSet negative = Minimal();
            negative.Achievements[0].rewardSouls = -1;
            MetaDataSet one = Minimal();
            one.Achievements[0].rewardSouls = 1;

            AssertFails(negative, "Achievement[A1] threshold");
            Assert.AreEqual(1, Build(one).Achievements[0].RewardSouls);
        }

        [TestCase(0, 3)]
        [TestCase(2, 0)]
        [TestCase(99, 3)]
        [TestCase(2, 99)]
        [TestCase(-1, -1)]
        public void 도전과제_이름_설명_키가_0_음수_없는_id면_위반이다(int nameKey, int descKey)
        {
            MetaDataSet data = Minimal();
            data.Achievements[0].nameKey = nameKey;
            data.Achievements[0].descKey = descKey;

            AssertFails(data, "Achievement[A1] nameKey " + nameKey + "·descKey " + descKey + " 가 Strings_Ko 에 없음");
        }

        [Test]
        public void 도전과제_표시_순서는_입력_행_순서와_같다()
        {
            MetaDataSet data = Minimal();
            data.Achievements = new[] { Achievement("Z"), Achievement("B"), Achievement("M") };

            MetaConfig meta = Build(data);

            Assert.AreEqual("Z", meta.Achievements[0].Id);
            Assert.AreEqual("B", meta.Achievements[1].Id);
            Assert.AreEqual("M", meta.Achievements[2].Id);
        }

        //# --- 위반 수집 ---

        [Test]
        public void 서로_다른_위반이_여러_개면_메시지에_전부_담긴다()
        {
            MetaDataSet data = Minimal();
            data.Config.xpGrowth = 0.5f;
            data.ShopItems = new[] { Item("Hp", "MonsterStat"), Item("Hp", "MonsterStat"), Item("Other", "Nope") };
            data.Stats = new[] { new ShopItem_StatRow { itemId = "Hp", statKind = "Bogus" } };
            data.Muls[0].perLevelMul = 0f;
            data.LordLevels = new[] { new LordLevelRow { level = 0 } };
            data.LordRewards = new[] { new LordLevel_RewardRow { level = 7, nameKey = 5, rewardSouls = 5 } };
            data.Achievements = new[] { Achievement("A1"), Achievement("A1") };

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            StringAssert.Contains("xpGrowth 는 1 이상이어야 함", e.Message);
            StringAssert.Contains("ShopItem.json itemId Hp 중복", e.Message);
            StringAssert.Contains("ShopItem[Other].effectKind 'Nope'", e.Message);
            StringAssert.Contains("ShopItem_Stat[Hp].statKind 'Bogus'", e.Message);
            StringAssert.Contains("ShopItem_Mul[Hp].perLevelMul", e.Message);
            StringAssert.Contains("LordLevel.json level 은 1 이상이어야 함", e.Message);
            StringAssert.Contains("LordLevel_Reward.json level 7", e.Message);
            StringAssert.Contains("Achievement.json achievementId A1 중복", e.Message);
        }

        //# --- 조회 ---

        [Test]
        public void FindShopItem은_id로_찾고_없거나_null이면_null이다()
        {
            MetaConfig meta = Build(Minimal());
            meta.ShopItems.Insert(0, null);

            Assert.AreEqual("Hp", meta.FindShopItem("Hp").Id);
            Assert.IsNull(meta.FindShopItem("hp"), "대소문자 구분");
            Assert.IsNull(meta.FindShopItem("Ghost"));
            Assert.IsNull(meta.FindShopItem(null));
        }
    }
}
